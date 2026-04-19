using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;
using SuperFluid.Internal.Services;

namespace SuperFluid.Tests.Services;

// Unit tests for CSharpLiteralFormatter — the pure-function seam extracted out of
// GrammarInterfaceReader. These tests target it directly (rather than end-to-end via a
// compiled grammar interface) so the formatting rules can be exercised without building a
// Roslyn compilation for every case.
public class CSharpLiteralFormatterTests
{
	// A stub ITypeSymbol is rarely needed; most cases only inspect the value. The few tests
	// that do care about the type (enum resolution) construct a real compilation.
	private static ITypeSymbol ObjectType { get; } = GetObjectType();

	private static ITypeSymbol GetObjectType()
	{
		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "Stub",
			references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) });
		return compilation.GetSpecialType(SpecialType.System_Object);
	}

	[Fact]
	public void FormatNullReturnsNullKeyword() =>
		CSharpLiteralFormatter.Format(null, ObjectType).ShouldBe("null");

	[Fact]
	public void FormatTrueReturnsLowercaseTrue() =>
		CSharpLiteralFormatter.Format(true, ObjectType).ShouldBe("true");

	[Fact]
	public void FormatFalseReturnsLowercaseFalse() =>
		CSharpLiteralFormatter.Format(false, ObjectType).ShouldBe("false");

	[Fact]
	public void FormatStringWrapsInDoubleQuotes() =>
		CSharpLiteralFormatter.Format("hello", ObjectType).ShouldBe("\"hello\"");

	[Fact]
	public void FormatStringEscapesSpecialCharacters() =>
		CSharpLiteralFormatter.Format("a\\b\"c\nd\te", ObjectType).ShouldBe("\"a\\\\b\\\"c\\nd\\te\"");

	[Fact]
	public void FormatEmptyStringProducesEmptyQuotedLiteral() =>
		CSharpLiteralFormatter.Format("", ObjectType).ShouldBe("\"\"");

	[Fact]
	public void FormatCharWrapsInSingleQuotes() =>
		CSharpLiteralFormatter.Format('x', ObjectType).ShouldBe("'x'");

	[Fact]
	public void FormatCharEscapesBackslash() =>
		CSharpLiteralFormatter.Format('\\', ObjectType).ShouldBe("'\\\\'");

	[Fact]
	public void FormatCharEscapesSingleQuote() =>
		CSharpLiteralFormatter.Format('\'', ObjectType).ShouldBe("'\\''");

	[Fact]
	public void FormatCharEscapesNewline() =>
		CSharpLiteralFormatter.Format('\n', ObjectType).ShouldBe("'\\n'");

	[Fact]
	public void FormatIntUsesBareNumericLiteral() =>
		CSharpLiteralFormatter.Format(42, ObjectType).ShouldBe("42");

	[Fact]
	public void FormatFloatAppendsFSuffix() =>
		CSharpLiteralFormatter.Format(3.14f, ObjectType).ShouldBe("3.14F");

	[Fact]
	public void FormatDoubleAppendsDSuffix() =>
		CSharpLiteralFormatter.Format(2.5, ObjectType).ShouldBe("2.5D");

	[Fact]
	public void FormatDecimalAppendsMSuffix() =>
		CSharpLiteralFormatter.Format(9.99m, ObjectType).ShouldBe("9.99M");

	[Fact]
	public void FormatLongAppendsLSuffix() =>
		CSharpLiteralFormatter.Format(5_000_000_000L, ObjectType).ShouldBe("5000000000L");

	[Fact]
	public void FormatUlongAppendsULSuffix() =>
		CSharpLiteralFormatter.Format(18_000_000_000_000_000_000UL, ObjectType).ShouldBe("18000000000000000000UL");

	[Fact]
	public void FormatUintAppendsUSuffix() =>
		CSharpLiteralFormatter.Format(4_000_000_000U, ObjectType).ShouldBe("4000000000U");

	[Fact]
	public void FormatByteFallsThroughToIConvertibleWithoutSuffix() =>
		// byte has no C# literal suffix — it implicitly converts from int literal, so a bare
		// number is correct. This confirms the generic IConvertible fallback handles sub-int
		// integer types cleanly.
		CSharpLiteralFormatter.Format((byte)5, ObjectType).ShouldBe("5");

	[Fact]
	public void FormatFloatUsesInvariantCultureSoDecimalSeparatorIsDot() =>
		// Guards against CultureInfo.CurrentCulture leaking in (e.g. German locale using ',')
		CSharpLiteralFormatter.Format(1.5f, ObjectType).ShouldBe("1.5F");

	[Fact]
	public void FormatDoubleRoundTripPreservesPrecisionAcrossSerialisationRoundTrip()
	{
		// Use a double whose "G" (default) ToString loses precision vs "R" — confirms the
		// formatter uses round-trip format. 0.1 + 0.2 prints as 0.3 with "G" but as
		// 0.30000000000000004 with "R".
		double lossy = 0.1 + 0.2;
		string formatted = CSharpLiteralFormatter.Format(lossy, ObjectType);
		formatted.ShouldBe("0.30000000000000004D");
	}

	[Fact]
	public void FormatNegativeIntPreservesMinusSign() =>
		CSharpLiteralFormatter.Format(-42, ObjectType).ShouldBe("-42");

	[Fact]
	public void FormatNegativeFloatPreservesMinusSignAndSuffix() =>
		CSharpLiteralFormatter.Format(-1.5f, ObjectType).ShouldBe("-1.5F");

	[Fact]
	public void FormatLongMinValueRoundTripsExactly() =>
		CSharpLiteralFormatter.Format(long.MinValue, ObjectType).ShouldBe("-9223372036854775808L");

	[Fact]
	public void FormatUlongMaxValueRoundTripsExactly() =>
		CSharpLiteralFormatter.Format(ulong.MaxValue, ObjectType).ShouldBe("18446744073709551615UL");

	[Fact]
	public void FormatStringEscapesCarriageReturn() =>
		CSharpLiteralFormatter.Format("a\rb", ObjectType).ShouldBe("\"a\\rb\"");

	[Fact]
	public void FormatStringPassesThroughNullByteUnescaped() =>
		// The formatter doesn't escape \0 — it's a known limitation worth pinning so a future
		// regression surfaces. If we ever add \0 support, update this test.
		CSharpLiteralFormatter.Format("a\0b", ObjectType).ShouldBe("\"a\0b\"");

	[Fact]
	public void FormatCharEscapesCarriageReturn() =>
		CSharpLiteralFormatter.Format('\r', ObjectType).ShouldBe("'\\r'");

	[Fact]
	public void FormatResolvesEnumMemberByValueForInt32Enum()
	{
		(INamedTypeSymbol enumType, int value) = GetEnumValue(@"
public enum Colour { Red = 1, Green = 2, Blue = 4 }
", "Colour", "Green");

		string result = CSharpLiteralFormatter.Format(value, enumType);

		result.ShouldBe("global::Colour.Green");
	}

	[Fact]
	public void FormatResolvesEnumMemberByValueForUInt64EnumAboveInt64Max()
	{
		(INamedTypeSymbol enumType, ulong value) = GetUlongEnumValue(@"
public enum BigFlags : ulong { None = 0, HighBit = 9223372036854775808UL }
", "BigFlags", "HighBit");

		string result = CSharpLiteralFormatter.Format(value, enumType);

		result.ShouldBe("global::BigFlags.HighBit");
	}

	[Fact]
	public void FormatFallsThroughToNumericLiteralWhenEnumValueHasNoMatchingMember()
	{
		(INamedTypeSymbol enumType, _) = GetEnumValue(@"
public enum Colour { Red = 1, Green = 2 }
", "Colour", "Red");

		// Pass a value that doesn't match any member — should fall through to the numeric path
		string result = CSharpLiteralFormatter.Format(99, enumType);

		result.ShouldBe("99");
	}

	// Compiles the given source and returns (enumType, underlyingIntValueOfMember).
	private static (INamedTypeSymbol EnumType, int Value) GetEnumValue(string source, string enumName, string memberName)
	{
		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "EnumTestAssembly",
			syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
			references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
			options: new(OutputKind.DynamicallyLinkedLibrary));

		INamedTypeSymbol enumType = compilation.GetTypeByMetadataName(enumName)!;
		enumType.ShouldNotBeNull();

		IFieldSymbol member = enumType.GetMembers()
			.OfType<IFieldSymbol>()
			.Single(f => f.Name == memberName);

		return (enumType, System.Convert.ToInt32(member.ConstantValue));
	}

	private static (INamedTypeSymbol EnumType, ulong Value) GetUlongEnumValue(string source, string enumName, string memberName)
	{
		CSharpCompilation compilation = CSharpCompilation.Create(
			assemblyName: "EnumTestAssembly",
			syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source) },
			references: new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) },
			options: new(OutputKind.DynamicallyLinkedLibrary));

		INamedTypeSymbol enumType = compilation.GetTypeByMetadataName(enumName)!;
		enumType.ShouldNotBeNull();

		IFieldSymbol member = enumType.GetMembers()
			.OfType<IFieldSymbol>()
			.Single(f => f.Name == memberName);

		return (enumType, System.Convert.ToUInt64(member.ConstantValue));
	}
}
