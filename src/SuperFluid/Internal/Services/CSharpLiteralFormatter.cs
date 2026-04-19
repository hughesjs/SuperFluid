using System;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SuperFluid.Internal.Services;

// Formats a boxed runtime value (the kind produced by IParameterSymbol.ExplicitDefaultValue or
// an attribute's TypedConstant) as the equivalent C# source literal. Pure function surface —
// no Roslyn-side effects, no grammar-interface-specific behaviour. Lives outside
// GrammarInterfaceReader so a future consumer (diagnostic quoting, migration tooling) can
// reuse it without pulling in the grammar-reader's other concerns.
internal static class CSharpLiteralFormatter
{
	// Returns the C# source text that would reproduce `value` as a literal in code. Handles
	// null, bool, string, char, enum member resolution, and numeric types with the correct
	// literal suffixes (float→F, double→D, decimal→M, long→L, ulong→UL, uint→U).
	// `parameterType` is used only to resolve enum members; other cases ignore it.
	public static string Format(object? value, ITypeSymbol parameterType)
	{
		if (value is null)
		{
			return "null";
		}

		if (value is bool boolValue)
		{
			return boolValue ? "true" : "false";
		}

		if (value is string stringValue)
		{
			return "\"" + EscapeStringLiteral(stringValue) + "\"";
		}

		if (value is char charValue)
		{
			return "'" + EscapeCharLiteral(charValue) + "'";
		}

		// ulong-backed enums with values above Int64.MaxValue would overflow Int64 conversion,
		// so branch on the underlying type and use UInt64 in that case.
		if (parameterType.TypeKind == TypeKind.Enum && value is IConvertible enumValue)
		{
			INamedTypeSymbol enumType = (INamedTypeSymbol)parameterType;
			string? enumMember = ResolveEnumMemberName(enumType, enumValue);
			if (enumMember is not null)
			{
				return enumMember;
			}
		}

		// Numeric types need explicit C# literal suffixes — bare IConvertible.ToString drops them,
		// producing e.g. `float x = 3.14` (CS1750) or `long x = 5000000000` (CS1021). Explicit
		// suffixes are always safe to emit even when optional (e.g. `42U` compiles the same as `42`).
		return value switch
		{
			float f   => f.ToString("R", CultureInfo.InvariantCulture) + "F",
			double d  => d.ToString("R", CultureInfo.InvariantCulture) + "D",
			decimal m => m.ToString(CultureInfo.InvariantCulture) + "M",
			long l    => l.ToString(CultureInfo.InvariantCulture) + "L",
			ulong ul  => ul.ToString(CultureInfo.InvariantCulture) + "UL",
			uint u    => u.ToString(CultureInfo.InvariantCulture) + "U",
			IConvertible c => c.ToString(CultureInfo.InvariantCulture),
			_ => value.ToString() ?? "null",
		};
	}

	private static string? ResolveEnumMemberName(INamedTypeSymbol enumType, IConvertible enumValue)
	{
		bool isUnsignedLong = enumType.EnumUnderlyingType?.SpecialType == SpecialType.System_UInt64;

		try
		{
			IFieldSymbol? member;
			if (isUnsignedLong)
			{
				ulong target = enumValue.ToUInt64(CultureInfo.InvariantCulture);
				member = enumType.GetMembers()
					.OfType<IFieldSymbol>()
					.FirstOrDefault(f => f.HasConstantValue && Convert.ToUInt64(f.ConstantValue, CultureInfo.InvariantCulture) == target);
			}
			else
			{
				long target = enumValue.ToInt64(CultureInfo.InvariantCulture);
				member = enumType.GetMembers()
					.OfType<IFieldSymbol>()
					.FirstOrDefault(f => f.HasConstantValue && Convert.ToInt64(f.ConstantValue, CultureInfo.InvariantCulture) == target);
			}

			return member is null ? null : $"global::{enumType.ToDisplayString()}.{member.Name}";
		}
		catch (OverflowException)
		{
			return null;
		}
	}

	private static string EscapeStringLiteral(string value)
	{
		StringBuilder sb = new(value.Length);
		foreach (char c in value)
		{
			switch (c)
			{
				case '\\': sb.Append("\\\\"); break;
				case '"':  sb.Append("\\\""); break;
				case '\n': sb.Append("\\n");  break;
				case '\r': sb.Append("\\r");  break;
				case '\t': sb.Append("\\t");  break;
				default:   sb.Append(c);      break;
			}
		}

		return sb.ToString();
	}

	private static string EscapeCharLiteral(char c)
	{
		return c switch
		{
			'\\' => "\\\\",
			'\'' => "\\'",
			'\n' => "\\n",
			'\r' => "\\r",
			'\t' => "\\t",
			_ => c.ToString()
		};
	}
}
