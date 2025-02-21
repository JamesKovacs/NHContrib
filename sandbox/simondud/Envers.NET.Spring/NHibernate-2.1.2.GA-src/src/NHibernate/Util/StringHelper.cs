using System;
using System.Collections;
using System.Text;
using NHibernate.SqlCommand;

namespace NHibernate.Util
{
	/// <summary></summary>
	public sealed class StringHelper
	{
		private StringHelper()
		{
			// not creatable
		}

		public const string WhiteSpace = " \n\r\f\t";

		/// <summary></summary>
		public const char Dot = '.';

		/// <summary></summary>
		public const char Underscore = '_';

		/// <summary></summary>
		public const string CommaSpace = ", ";

		/// <summary></summary>
		public const string Comma = ",";

		/// <summary></summary>
		public const string OpenParen = "(";

		/// <summary></summary>
		public const string ClosedParen = ")";

		/// <summary></summary>
		public const char SingleQuote = '\'';

		/// <summary></summary>
		public const string SqlParameter = "?";

		public const int AliasTruncateLength = 10;

		public static string Join(string separator, IEnumerable objects)
		{
			StringBuilder buf = new StringBuilder();
			bool first = true;

			foreach (object obj in objects)
			{
				if (!first)
				{
					buf.Append(separator);
				}

				first = false;
				buf.Append(obj);
			}

			return buf.ToString();
		}

		public static SqlString Join(SqlString separator, IEnumerable objects)
		{
			SqlStringBuilder buf = new SqlStringBuilder();
			bool first = true;

			foreach (object obj in objects)
			{
				if (!first)
				{
					buf.Add(separator);
				}

				first = false;
				buf.AddObject(obj);
			}

			return buf.ToSqlString();
		}

		public static SqlString[] Add(SqlString[] x, string sep, SqlString[] y)
		{
			SqlString[] result = new SqlString[x.Length];
			for (int i = 0; i < x.Length; i++)
			{
				result[i] = new SqlStringBuilder(3)
					.Add(x[i])
					.Add(sep)
					.Add(y[i])
					.ToSqlString();
			}
			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="str"></param>
		/// <param name="times"></param>
		/// <returns></returns>
		public static string Repeat(string str, int times)
		{
			StringBuilder buf = new StringBuilder(str.Length * times);
			for (int i = 0; i < times; i++)
			{
				buf.Append(str);
			}
			return buf.ToString();
		}

		public static string Replace(string template, string placeholder, string replacement)
		{
			return Replace(template, placeholder, replacement, false);
		}

		public static string Replace(string template, string placeholder, string replacement, bool wholeWords)
		{
			// sometimes a null value will get passed in here -> SqlWhereStrings are a good example
			if (template == null)
			{
				return null;
			}

			int loc = template.IndexOf(placeholder);
			if (loc < 0)
			{
				return template;
			}
			else
			{
				// NH different implementation (NH-1253)
				string replaceWith = replacement;
				if(loc + placeholder.Length < template.Length)
				{
					string afterPlaceholder = template[loc + placeholder.Length].ToString();
					//After a token in HQL there can be whitespace, closedparen or comma.. 
					if(wholeWords && !(WhiteSpace.Contains(afterPlaceholder) || ClosedParen.Equals(afterPlaceholder) || Comma.Equals(afterPlaceholder)))
					{
						//If this is not a full token we don't want to touch it
						replaceWith = placeholder;
					}
				}

				return
					new StringBuilder(template.Substring(0, loc)).Append(replaceWith).Append(
						Replace(template.Substring(loc + placeholder.Length), placeholder, replacement, wholeWords)).ToString();
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="template"></param>
		/// <param name="placeholder"></param>
		/// <param name="replacement"></param>
		/// <returns></returns>
		public static string ReplaceOnce(string template, string placeholder, string replacement)
		{
			int loc = template.IndexOf(placeholder);
			if (loc < 0)
			{
				return template;
			}
			else
			{
				return new StringBuilder(template.Substring(0, loc))
					.Append(replacement)
					.Append(template.Substring(loc + placeholder.Length))
					.ToString();
			}
		}

		/// <summary>
		/// Just a fa�ade for calling string.Split()
		/// We don't use our StringTokenizer because string.Split() is
		/// more efficient (but it only works when we don't want to retrieve the delimiters)
		/// </summary>
		/// <param name="separators">separators for the tokens of the list</param>
		/// <param name="list">the string that will be broken into tokens</param>
		/// <returns></returns>
		public static string[] Split(string separators, string list)
		{
			return list.Split(separators.ToCharArray());
		}

		/// <summary>
		/// Splits the String using the StringTokenizer.  
		/// </summary>
		/// <param name="separators">separators for the tokens of the list</param>
		/// <param name="list">the string that will be broken into tokens</param>
		/// <param name="include">true to include the separators in the tokens.</param>
		/// <returns></returns>
		/// <remarks>
		/// This is more powerful than Split because you have the option of including or 
		/// not including the separators in the tokens.
		/// </remarks>
		public static string[] Split(string separators, string list, bool include)
		{
			StringTokenizer tokens = new StringTokenizer(list, separators, include);
			ArrayList results = new ArrayList();
			foreach (string token in tokens)
			{
				results.Add(token);
			}
			return (string[])results.ToArray(typeof(string));
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="qualifiedName"></param>
		/// <returns></returns>
		public static string Unqualify(string qualifiedName)
		{
			if(qualifiedName.IndexOf('`') > 0)
			{
				// less performance but correctly manage generics classes
				// where the entity-name was not specified
				// Note: the enitty-name is mandatory when the user want work with different type-args
				// for the same generic-entity implementation
				return GetClassname(qualifiedName);
			}
			return Unqualify(qualifiedName, ".");
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="qualifiedName"></param>
		/// <param name="seperator"></param>
		/// <returns></returns>
		public static string Unqualify(string qualifiedName, string seperator)
		{
			return qualifiedName.Substring(qualifiedName.LastIndexOf(seperator) + 1);
		}

		/// <summary>
		/// Takes a fully qualified type name and returns the full name of the 
		/// Class - includes namespaces.
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		public static string GetFullClassname(string typeName)
		{
			return new TypeNameParser(null, null).ParseTypeName(typeName).Type;
		}

		/// <summary>
		/// Takes a fully qualified type name (can include the assembly) and just returns
		/// the name of the Class.
		/// </summary>
		/// <param name="typeName"></param>
		/// <returns></returns>
		public static string GetClassname(string typeName)
		{
			//string[] splitClassname = GetFullClassname(typeName).Split('.');
			string fullClassName = GetFullClassname(typeName);

			int genericTick = fullClassName.IndexOf('`');
			if (genericTick != -1)
			{
				string nameBeforeGenericTick = fullClassName.Substring(0, genericTick);
				int lastPeriod = nameBeforeGenericTick.LastIndexOf('.');
				return lastPeriod != -1 ? fullClassName.Substring(lastPeriod + 1) : fullClassName;
			}
			string[] splitClassname = fullClassName.Split('.');
			return splitClassname[splitClassname.Length - 1];
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="qualifiedName"></param>
		/// <returns></returns>
		public static string Qualifier(string qualifiedName)
		{
			int loc = qualifiedName.LastIndexOf(".");
			if (loc < 0)
			{
				return String.Empty;
			}
			else
			{
				return qualifiedName.Substring(0, loc);
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="columns"></param>
		/// <param name="suffix"></param>
		/// <returns></returns>
		public static string[] Suffix(string[] columns, string suffix)
		{
			if (suffix == null)
			{
				return columns;
			}
			string[] qualified = new string[columns.Length];
			for (int i = 0; i < columns.Length; i++)
			{
				qualified[i] = Suffix(columns[i], suffix);
			}
			return qualified;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="name"></param>
		/// <param name="suffix"></param>
		/// <returns></returns>
		public static string Suffix(string name, string suffix)
		{
			return (suffix == null) ?
				   name :
				   name + suffix;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="columns"></param>
		/// <param name="prefix"></param>
		/// <returns></returns>
		public static string[] Prefix(string[] columns, string prefix)
		{
			if (prefix == null)
			{
				return columns;
			}
			string[] qualified = new string[columns.Length];
			for (int i = 0; i < columns.Length; i++)
			{
				qualified[i] = prefix + columns[i];
			}
			return qualified;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="qualifiedName"></param>
		/// <returns></returns>
		public static string Root(string qualifiedName)
		{
			int loc = qualifiedName.IndexOf(".");
			return (loc < 0)
					? qualifiedName
					: qualifiedName.Substring(0, loc);
		}

		/// <summary>
		/// Converts a <see cref="String"/> in the format of "true", "t", "false", or "f" to
		/// a <see cref="Boolean"/>.
		/// </summary>
		/// <param name="value">The string to convert.</param>
		/// <returns>
		/// The <c>value</c> converted to a <see cref="Boolean"/> .
		/// </returns>
		public static bool BooleanValue(string value)
		{
			string trimmed = value.Trim().ToLowerInvariant();
			return trimmed.Equals("true") || trimmed.Equals("t");
		}

		private static string NullSafeToString(object obj)
		{
			return obj == null ? "(null)" : obj.ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="array"></param>
		/// <returns></returns>
		public static string ToString(object[] array)
		{
			int len = array.Length;

			// if there is no value in the array then return no string...
			if (len == 0)
			{
				return String.Empty;
			}

			StringBuilder buf = new StringBuilder(len * 12);
			for (int i = 0; i < len - 1; i++)
			{
				buf.Append(NullSafeToString(array[i])).Append(CommaSpace);
			}
			return buf.Append(NullSafeToString(array[len - 1])).ToString();
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="str"></param>
		/// <param name="placeholders"></param>
		/// <param name="replacements"></param>
		/// <returns></returns>
		public static string[] Multiply(string str, IEnumerator placeholders, IEnumerator replacements)
		{
			string[] result = new string[] { str };
			while (placeholders.MoveNext())
			{
				replacements.MoveNext();
				result = Multiply(result, placeholders.Current as string, replacements.Current as string[]);
			}
			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="strings"></param>
		/// <param name="placeholder"></param>
		/// <param name="replacements"></param>
		/// <returns></returns>
		public static string[] Multiply(string[] strings, string placeholder, string[] replacements)
		{
			string[] results = new string[replacements.Length * strings.Length];
			int n = 0;
			for (int i = 0; i < replacements.Length; i++)
			{
				for (int j = 0; j < strings.Length; j++)
				{
					results[n++] = ReplaceOnce(strings[j], placeholder, replacements[i]);
				}
			}
			return results;
		}

		/// <summary>
		/// Counts the unquoted instances of the character.
		/// </summary>
		/// <param name="str"></param>
		/// <param name="character"></param>
		/// <returns></returns>
		public static int CountUnquoted(string str, char character)
		{
			if (SingleQuote == character)
			{
				throw new ArgumentOutOfRangeException("character", "Unquoted count of quotes is invalid");
			}

			// Impl note: takes advantage of the fact that an escaped single quote
			// embedded within a quote-block can really be handled as two separate
			// quote-blocks for the purposes of this method...
			int count = 0;
			char[] chars = str.ToCharArray();
			int stringLength = string.IsNullOrEmpty(str) ? 0 : chars.Length;
			bool inQuote = false;
			for (int indx = 0; indx < stringLength; indx++)
			{
				if (inQuote)
				{
					if (SingleQuote == chars[indx])
					{
						inQuote = false;
					}
				}
				else if (SingleQuote == chars[indx])
				{
					inQuote = true;
				}
				else if (chars[indx] == character)
				{
					count++;
				}
			}
			return count;
		}

		public static bool IsEmpty(string str)
		{
			return string.IsNullOrEmpty(str);
		}

		public static bool IsNotEmpty(string str)
		{
			return !IsEmpty(str);
		}

		public static bool IsNotEmpty(SqlString str)
		{
			return str != null && str.Count > 0;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name="prefix"></param>
		/// <param name="name"></param>
		/// <returns></returns>
		public static string Qualify(string prefix, string name)
		{
			char first = name[0];

			// Should we check for prefix == string.Empty rather than a length check?
			if (!string.IsNullOrEmpty(prefix) && first != SingleQuote && !char.IsDigit(first))
			{
				return prefix + Dot + name;
			}
			else
			{
				return name;
			}
		}

		public static string[] Qualify(string prefix, string[] names)
		{
			// Should we check for prefix == string.Empty rather than a length check?
			if (!string.IsNullOrEmpty(prefix))
			{
				int len = names.Length;
				string[] qualified = new string[len];
				for (int i = 0; i < len; i++)
				{
					qualified[i] = Qualify(prefix, names[i]);
				}
				return qualified;
			}
			else
			{
				return names;
			}
		}

		public static int FirstIndexOfChar(string sqlString, string str, int startIndex)
		{
			return sqlString.IndexOfAny(str.ToCharArray(), startIndex);
		}

		public static string Truncate(string str, int length)
		{
			if (str.Length <= length)
			{
				return str;
			}
			else
			{
				return str.Substring(0, length);
			}
		}

		public static int LastIndexOfLetter(string str)
		{
			for (int i = 0; i < str.Length; i++)
			{
				if (!char.IsLetter(str, i) /*&& !('_'==character)*/)
				{
					return i - 1;
				}
			}
			return str.Length - 1;
		}

		public static string UnqualifyEntityName(string entityName)
		{
			string result = Unqualify(entityName);
			int slashPos = result.IndexOf('/');
			if (slashPos > 0)
			{
				result = result.Substring(0, slashPos - 1);
			}
			return result;
		}

		public static string GenerateAlias(string description)
		{
			return GenerateAliasRoot(description) + Underscore;
		}

		/// <summary>
		/// Generate a nice alias for the given class name or collection role
		/// name and unique integer. Subclasses do <em>not</em> have to use
		/// aliases of this form.
		/// </summary>
		/// <returns>an alias of the form <c>foo1_</c></returns>
		public static string GenerateAlias(string description, int unique)
		{
			return GenerateAliasRoot(description) +
				   unique +
				   Underscore;
		}

		private static string GenerateAliasRoot(string description)
		{
			// Remove any generic arguments attached to description
			int indexOfBacktick = description.IndexOf('`');
			if (indexOfBacktick > 0)
			{
				description = Truncate(description, indexOfBacktick);
			}

			string result = Truncate(UnqualifyEntityName(description), AliasTruncateLength)
				.ToLowerInvariant()
				.Replace('/', '_') // entityNames may now include slashes for the representations
				.Replace('+', '_') // classname may be an inner class
				.Replace('[', '_') // classname may contain brackets
				.Replace(']', '_')
				.Replace('`', '_') // classname may contain backticks (generic types)
				.TrimStart('_') // Remove underscores from the beginning of the alias (for Firebird).
				;

			if (char.IsDigit(result, result.Length - 1))
			{
				return result + "x"; //ick!
			}
			else
			{
				return result;
			}
		}

		public static string MoveAndToBeginning(string filter)
		{
			if (filter.Trim().Length > 0)
			{
				filter += " and ";
				if (filter.StartsWith(" and "))
				{
					filter = filter.Substring(4);
				}
			}
			return filter;
		}

		public static string Unroot(string qualifiedName)
		{
			int loc = qualifiedName.IndexOf(".");
			return (loc < 0) ? qualifiedName : qualifiedName.Substring(loc + 1);
		}

		public static bool EqualsCaseInsensitive(string a, string b)
		{
			return StringComparer.InvariantCultureIgnoreCase.Compare(a, b) == 0;
		}

		public static int IndexOfCaseInsensitive(string source, string value)
		{
			return source.IndexOf(value, StringComparison.InvariantCultureIgnoreCase);
		}

		public static int IndexOfCaseInsensitive(string source, string value, int startIndex)
		{
			return source.IndexOf(value, startIndex, StringComparison.InvariantCultureIgnoreCase);
		}

		public static int IndexOfCaseInsensitive(string source, string value, int startIndex, int count)
		{
			return source.IndexOf(value, startIndex, count, StringComparison.InvariantCultureIgnoreCase);
		}

		public static int LastIndexOfCaseInsensitive(string source, string value)
		{
			return source.LastIndexOf(value, StringComparison.InvariantCultureIgnoreCase);
		}

		public static bool StartsWithCaseInsensitive(string source, string prefix)
		{
			return source.StartsWith(prefix, StringComparison.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Returns the interned string equal to <paramref name="str"/> if there is one, or <paramref name="str"/>
		/// otherwise.
		/// </summary>
		/// <param name="str">A <see cref="string" /></param>
		/// <returns>A <see cref="string" /></returns>
		public static string InternedIfPossible(string str)
		{
			if (str == null)
			{
				return null;
			}

			string interned = string.IsInterned(str);
			if (interned != null)
			{
				return interned;
			}

			return str;
		}

		public static string CollectionToString(ICollection keys)
		{
			StringBuilder sb = new StringBuilder();
			foreach (object o in keys)
			{
				sb.Append(o);
				sb.Append(", ");
			}
			if (sb.Length != 0)//remove last ", "
				sb.Remove(sb.Length - 2, 2);
			return sb.ToString();

		}

		public static SqlString RemoveAsAliasesFromSql(SqlString sql)
		{
			return sql.Substring(0, sql.LastIndexOfCaseInsensitive(" as "));
		}


		public static string ToUpperCase(string str)
		{
			return str == null ? null : str.ToUpperInvariant();
		}

		public static string ToLowerCase(string str)
		{
			return str == null ? null : str.ToLowerInvariant();
		}

		public static bool IsBackticksEnclosed(string identifier)
		{
			return !string.IsNullOrEmpty(identifier) && identifier.StartsWith("`") && identifier.EndsWith("`");
		}

		public static string PurgeBackticksEnclosing(string identifier)
		{
			if (IsBackticksEnclosed(identifier))
			{
				return identifier.Substring(1, identifier.Length - 2);
			}
			return identifier;
		}

		public static string[] ParseFilterParameterName(string filterParameterName)
		{
			int dot = filterParameterName.IndexOf(".");
			if (dot <= 0)
			{
				throw new ArgumentException("Invalid filter-parameter name format; the name should be a property path.", "filterParameterName");
			}
			string filterName = filterParameterName.Substring(0, dot);
			string parameterName = filterParameterName.Substring(dot + 1);
			return new[] { filterName, parameterName };
		}
	}
}
