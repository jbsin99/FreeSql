﻿using FreeSql.Internal;
using FreeSql.Internal.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace FreeSql.Oracle {
	class OracleExpression : CommonExpression {

		public OracleExpression(CommonUtils common) : base(common) { }

		internal override string ExpressionLambdaToSqlOther(Expression exp, List<SelectTableInfo> _tables, List<SelectColumnInfo> _selectColumnMap, Func<Expression[], string> getSelectGroupingMapString, SelectTableInfoType tbtype, bool isQuoteName) {
			Func<Expression, string> getExp = exparg => ExpressionLambdaToSql(exparg, _tables, _selectColumnMap, getSelectGroupingMapString, tbtype, isQuoteName);
			switch (exp.NodeType) {
				case ExpressionType.Call:
					var callExp = exp as MethodCallExpression;
					var objExp = callExp.Object;
					var objType = objExp?.Type;
					if (objType?.FullName == "System.Byte[]") return null;

					var argIndex = 0;
					if (objType == null && callExp.Method.DeclaringType.FullName == typeof(Enumerable).FullName) {
						objExp = callExp.Arguments.FirstOrDefault();
						objType = objExp?.Type;
						argIndex++;
					}
					if (objType == null) objType = callExp.Method.DeclaringType;
					if (objType != null) {
						var left = objExp == null ? null : getExp(objExp);
						if (objType.IsArray == true) {
							switch (callExp.Method.Name) {
								case "Contains":
									//判断 in
									return $"({getExp(callExp.Arguments[argIndex])}) in {left}";
							}
						}
					}
					break;
				case ExpressionType.NewArrayInit:
					var arrExp = exp as NewArrayExpression;
					var arrSb = new StringBuilder();
					arrSb.Append("(");
					for (var a = 0; a < arrExp.Expressions.Count; a++) {
						if (a > 0) arrSb.Append(",");
						arrSb.Append(getExp(arrExp.Expressions[a]));
					}
					return arrSb.Append(")").ToString();
			}
			return null;
		}

		internal override string ExpressionLambdaToSqlMemberAccessString(MemberExpression exp, List<SelectTableInfo> _tables, List<SelectColumnInfo> _selectColumnMap, Func<Expression[], string> getSelectGroupingMapString, SelectTableInfoType tbtype, bool isQuoteName) {
			if (exp.Expression == null) {
				switch (exp.Member.Name) {
					case "Empty": return "''";
				}
				return null;
			}
			var left = ExpressionLambdaToSql(exp.Expression, _tables, _selectColumnMap, getSelectGroupingMapString, tbtype, isQuoteName);
			switch (exp.Member.Name) {
				case "Length": return $"length({left})";
			}
			return null;
		}
		internal override string ExpressionLambdaToSqlMemberAccessDateTime(MemberExpression exp, List<SelectTableInfo> _tables, List<SelectColumnInfo> _selectColumnMap, Func<Expression[], string> getSelectGroupingMapString, SelectTableInfoType tbtype, bool isQuoteName) {
			if (exp.Expression == null) {
				switch (exp.Member.Name) {
					case "Now": return "systimestamp";
					case "UtcNow": return "sys_extract_utc(systimestamp)";
					case "Today": return "trunc(systimestamp)";
					case "MinValue": return "to_timestamp('0001-01-01 00:00:00','YYYY-MM-DD HH24:MI:SS.FF6')";
					case "MaxValue": return "to_timestamp('9999-12-31 23:59:59','YYYY-MM-DD HH24:MI:SS.FF6')";
				}
				return null;
			}
			var left = ExpressionLambdaToSql(exp.Expression, _tables, _selectColumnMap, getSelectGroupingMapString, tbtype, isQuoteName);
			switch (exp.Member.Name) {
				case "Date": return $"trunc({left})";
				case "TimeOfDay": return $"({left}-trunc({left}))";
				case "DayOfWeek": return $"case when to_char({left})='7' then 0 else cast(to_char({left}) as number) end";
				case "Day": return $"cast(to_char({left},'DD') as number)";
				case "DayOfYear": return $"cast(to_char({left},'DDD') as number)";
				case "Month": return $"cast(to_char({left},'MM') as number)";
				case "Year": return $"cast(to_char({left},'YYYY') as number)";
				case "Hour": return $"cast(to_char({left},'HH24') as number)";
				case "Minute": return $"cast(to_char({left},'MI') as number)";
				case "Second": return $"cast(to_char({left},'SS') as number)";
				case "Millisecond": return $"cast(to_char({left},'FF3') as number)";
				case "Ticks": return $"cast(to_char({left},'FF7') as number)";
			}
			return null;
		}
		internal override string ExpressionLambdaToSqlMemberAccessTimeSpan(MemberExpression exp, List<SelectTableInfo> _tables, List<SelectColumnInfo> _selectColumnMap, Func<Expression[], string> getSelectGroupingMapString, SelectTableInfoType tbtype, bool isQuoteName) {
			if (exp.Expression == null) {
				switch (exp.Member.Name) {
					case "Zero": return "numtodsinterval(0,'second')";
					case "MinValue": return "numtodsinterval(-233720368.5477580,'second')";
					case "MaxValue": return "numtodsinterval(233720368.5477580,'second')";
				}
				return null;
			}
			var left = ExpressionLambdaToSql(exp.Expression, _tables, _selectColumnMap, getSelectGroupingMapString, tbtype, isQuoteName);
			switch (exp.Member.Name) {
				case "Days": return $"extract(day from {left})";
				case "Hours": return $"extract(hour from {left})";
				case "Milliseconds": return $"cast(substr(extract(second from {left})-floor(extract(second from {left})),2,3) as number)";
				case "Minutes": return $"extract(minute from {left})";
				case "Seconds": return $"floor(extract(second from {left}))";
				case "Ticks": return $"(extract(day from {left})*86400+extract(hour from {left})*3600+extract(minute from {left})*60+extract(second from {left}))*10000000";
				case "TotalDays": return $"extract(day from {left})";
				case "TotalHours": return $"(extract(day from {left})*24+extract(hour from {left}))";
				case "TotalMilliseconds": return $"(extract(day from {left})*86400+extract(hour from {left})*3600+extract(minute from {left})*60+extract(second from {left}))*1000";
				case "TotalMinutes": return $"(extract(day from {left})*1440+extract(hour from {left})*60+extract(minute from {left}))";
				case "TotalSeconds": return $"(extract(day from {left})*86400+extract(hour from {left})*3600+extract(minute from {left})*60+extract(second from {left}))";
			}
			return null;
		}

		internal override string ExpressionLambdaToSqlCallString(MethodCallExpression exp, List<SelectTableInfo> _tables, List<SelectColumnInfo> _selectColumnMap, Func<Expression[], string> getSelectGroupingMapString, SelectTableInfoType tbtype, bool isQuoteName) {
			Func<Expression, string> getExp = exparg => ExpressionLambdaToSql(exparg, _tables, _selectColumnMap, getSelectGroupingMapString, tbtype, isQuoteName);
			if (exp.Object == null) {
				switch (exp.Method.Name) {
					case "IsNullOrEmpty":
						var arg1 = getExp(exp.Arguments[0]);
						return $"({arg1} is null or {arg1} = '')";
				}
			} else {
				var left = getExp(exp.Object);
				switch (exp.Method.Name) {
					case "StartsWith":
					case "EndsWith":
					case "Contains":
						var args0Value = getExp(exp.Arguments[0]);
						if (args0Value == "NULL") return $"({left}) IS NULL";
						if (exp.Method.Name == "StartsWith") return $"({left}) LIKE {(args0Value.EndsWith("'") ? args0Value.Insert(args0Value.Length - 1, "%") : $"(to_char({args0Value})||'%')")}";
						if (exp.Method.Name == "EndsWith") return $"({left}) LIKE {(args0Value.StartsWith("'") ? args0Value.Insert(1, "%") : $"('%'||to_char({args0Value}))")}";
						if (args0Value.StartsWith("'") && args0Value.EndsWith("'")) return $"({left}) LIKE {args0Value.Insert(1, "%").Insert(args0Value.Length, "%")}";
						return $"({left}) LIKE ('%'||to_char({args0Value})||'%')";
					case "ToLower": return $"lower({left})";
					case "ToUpper": return $"upper({left})";
					case "Substring":
						var substrArgs1 = getExp(exp.Arguments[0]);
						if (long.TryParse(substrArgs1, out var testtrylng1)) substrArgs1 = (testtrylng1 + 1).ToString();
						else substrArgs1 += "+1";
						if (exp.Arguments.Count == 1) return $"substr({left}, {substrArgs1})";
						return $"substr({left}, {substrArgs1}, {getExp(exp.Arguments[1])})";
					case "IndexOf":
						var indexOfFindStr = getExp(exp.Arguments[0]);
						if (exp.Arguments.Count > 1 && exp.Arguments[1].Type.FullName == "System.Int32") {
							var locateArgs1 = getExp(exp.Arguments[1]);
							if (long.TryParse(locateArgs1, out var testtrylng2)) locateArgs1 = (testtrylng2 + 1).ToString();
							else locateArgs1 += "+1";
							return $"(instr({left}, {indexOfFindStr}, {locateArgs1}, 1)-1)";
						}
						return $"(instr({left}, {indexOfFindStr}, 1, 1))-1";
					case "PadLeft":
						if (exp.Arguments.Count == 1) return $"lpad({left}, {getExp(exp.Arguments[0])}, ' ')";
						return $"lpad({left}, {getExp(exp.Arguments[0])}, {getExp(exp.Arguments[1])})";
					case "PadRight":
						if (exp.Arguments.Count == 1) return $"rpad({left}, {getExp(exp.Arguments[0])}, ' ')";
						return $"rpad({left}, {getExp(exp.Arguments[0])}, {getExp(exp.Arguments[1])})";
					case "Trim":
					case "TrimStart":
					case "TrimEnd":
						if (exp.Arguments.Count == 0) {
							if (exp.Method.Name == "Trim") return $"trim({left})";
							if (exp.Method.Name == "TrimStart") return $"ltrim({left})";
							if (exp.Method.Name == "TrimEnd") return $"rtrim({left})";
						}
						foreach (var argsTrim02 in exp.Arguments) {
							var argsTrim01s = new[] { argsTrim02 };
							if (argsTrim02.NodeType == ExpressionType.NewArrayInit) {
								var arritem = argsTrim02 as NewArrayExpression;
								argsTrim01s = arritem.Expressions.ToArray();
							}
							foreach (var argsTrim01 in argsTrim01s) {
								if (exp.Method.Name == "Trim") left = $"trim(both {getExp(argsTrim01)} from {left})";
								if (exp.Method.Name == "TrimStart") left = $"ltrim({left},{getExp(argsTrim01)})";
								if (exp.Method.Name == "TrimEnd") left = $"rtrim({left},{getExp(argsTrim01)})";
							}
						}
						return left;
					case "Replace": return $"replace({left}, {getExp(exp.Arguments[0])}, {getExp(exp.Arguments[1])})";
					case "CompareTo": return $"case when {left} = {getExp(exp.Arguments[0])} then 0 when {left} > {getExp(exp.Arguments[0])} then 1 else -1 end";
					case "Equals": return $"({left} = {getExp(exp.Arguments[0])})";
				}
			}
			throw new Exception($"OracleExpression 未现实函数表达式 {exp} 解析");
		}
		internal override string ExpressionLambdaToSqlCallMath(MethodCallExpression exp, List<SelectTableInfo> _tables, List<SelectColumnInfo> _selectColumnMap, Func<Expression[], string> getSelectGroupingMapString, SelectTableInfoType tbtype, bool isQuoteName) {
			Func<Expression, string> getExp = exparg => ExpressionLambdaToSql(exparg, _tables, _selectColumnMap, getSelectGroupingMapString, tbtype, isQuoteName);
			switch (exp.Method.Name) {
				case "Abs": return $"abs({getExp(exp.Arguments[0])})";
				case "Sign": return $"sign({getExp(exp.Arguments[0])})";
				case "Floor": return $"floor({getExp(exp.Arguments[0])})";
				case "Ceiling": return $"ceil({getExp(exp.Arguments[0])})";
				case "Round":
					if (exp.Arguments.Count > 1 && exp.Arguments[1].Type.FullName == "System.Int32") return $"round({getExp(exp.Arguments[0])}, {getExp(exp.Arguments[1])})";
					return $"round({getExp(exp.Arguments[0])})";
				case "Exp": return $"exp({getExp(exp.Arguments[0])})";
				case "Log": if (exp.Arguments.Count > 1) return $"log({getExp(exp.Arguments[1])},{getExp(exp.Arguments[0])})";
					return $"log(2.7182818284590451,{getExp(exp.Arguments[0])})";
				case "Log10": return $"log(10,{getExp(exp.Arguments[0])})";
				case "Pow": return $"power({getExp(exp.Arguments[0])}, {getExp(exp.Arguments[1])})";
				case "Sqrt": return $"sqrt({getExp(exp.Arguments[0])})";
				case "Cos": return $"cos({getExp(exp.Arguments[0])})";
				case "Sin": return $"sin({getExp(exp.Arguments[0])})";
				case "Tan": return $"tan({getExp(exp.Arguments[0])})";
				case "Acos": return $"acos({getExp(exp.Arguments[0])})";
				case "Asin": return $"asin({getExp(exp.Arguments[0])})";
				case "Atan": return $"atan({getExp(exp.Arguments[0])})";
				//case "Atan2": return $"atan2({getExp(exp.Arguments[0])}, {getExp(exp.Arguments[1])})";
				case "Truncate": return $"trunc({getExp(exp.Arguments[0])}, 0)";
			}
			throw new Exception($"OracleExpression 未现实函数表达式 {exp} 解析");
		}
		internal override string ExpressionLambdaToSqlCallDateTime(MethodCallExpression exp, List<SelectTableInfo> _tables, List<SelectColumnInfo> _selectColumnMap, Func<Expression[], string> getSelectGroupingMapString, SelectTableInfoType tbtype, bool isQuoteName) {
			Func<Expression, string> getExp = exparg => ExpressionLambdaToSql(exparg, _tables, _selectColumnMap, getSelectGroupingMapString, tbtype, isQuoteName);
			if (exp.Object == null) {
				switch (exp.Method.Name) {
					case "Compare": return $"extract(day from ({getExp(exp.Arguments[0])}-({getExp(exp.Arguments[1])})))";
					case "DaysInMonth": return $"cast(to_char(last_day(({getExp(exp.Arguments[0])})||'-'||({getExp(exp.Arguments[1])})||'-01'),'DD') as number)";
					case "Equals": return $"({getExp(exp.Arguments[0])} = {getExp(exp.Arguments[1])})";

					case "IsLeapYear":
						var isLeapYearArgs1 = getExp(exp.Arguments[0]);
						return $"(mod({isLeapYearArgs1},4)=0 AND mod({isLeapYearArgs1},100)<>0 OR mod({isLeapYearArgs1},400)=0)";

					case "Parse": return $"to_timestamp({getExp(exp.Arguments[0])},'YYYY-MM-DD HH24:MI:SS.FF6')";
					case "ParseExact":
					case "TryParse":
					case "TryParseExact": return $"to_timestamp({getExp(exp.Arguments[0])},'YYYY-MM-DD HH24:MI:SS.FF6')";
				}
			} else {
				var left = getExp(exp.Object);
				var args1 = exp.Arguments.Count == 0 ? null : getExp(exp.Arguments[0]);
				switch (exp.Method.Name) {
					case "Add": return $"({left}+{args1})";
					case "AddDays": return $"({left}+{args1})";
					case "AddHours": return $"({left}+({args1})/24)";
					case "AddMilliseconds": return $"({left}+({args1})/86400000)";
					case "AddMinutes": return $"({left}+({args1})/1440)";
					case "AddMonths": return $"add_months({left},{args1})";
					case "AddSeconds": return $"({left}+({args1})/86400)";
					case "AddTicks": return $"({left}+({args1})/864000000000)";
					case "AddYears": return $"add_months({left},({args1})*12)";
					case "Subtract":
						if (exp.Arguments[0].Type.FullName == "System.DateTime" || exp.Arguments[0].Type.GenericTypeArguments.FirstOrDefault()?.FullName == "System.DateTime")
							return $"({args1}-{left})";
						if (exp.Arguments[0].Type.FullName == "System.TimeSpan" || exp.Arguments[0].Type.GenericTypeArguments.FirstOrDefault()?.FullName == "System.TimeSpan")
							return $"({left}-{args1})";
						break;
					case "Equals": return $"({left} = {getExp(exp.Arguments[0])})";
					case "CompareTo": return $"extract(day from ({left}-({getExp(exp.Arguments[0])})))";
					case "ToString": return $"to_char({left},'YYYY-MM-DD HH24:MI:SS.FF6')";
				}
			}
			throw new Exception($"OracleExpression 未现实函数表达式 {exp} 解析");
		}
		internal override string ExpressionLambdaToSqlCallTimeSpan(MethodCallExpression exp, List<SelectTableInfo> _tables, List<SelectColumnInfo> _selectColumnMap, Func<Expression[], string> getSelectGroupingMapString, SelectTableInfoType tbtype, bool isQuoteName) {
			Func<Expression, string> getExp = exparg => ExpressionLambdaToSql(exparg, _tables, _selectColumnMap, getSelectGroupingMapString, tbtype, isQuoteName);
			if (exp.Object == null) {
				switch (exp.Method.Name) {
					case "Compare": return $"extract(day from ({getExp(exp.Arguments[0])}-({getExp(exp.Arguments[1])})))";
					case "Equals": return $"({getExp(exp.Arguments[0])} = {getExp(exp.Arguments[1])})";
					case "FromDays": return $"numtodsinterval(({getExp(exp.Arguments[0])})*{(long)60 * 60 * 24},'second')";
					case "FromHours": return $"numtodsinterval(({getExp(exp.Arguments[0])})*{(long)60 * 60},'second')";
					case "FromMilliseconds": return $"numtodsinterval(({getExp(exp.Arguments[0])})/1000,'second')";
					case "FromMinutes": return $"numtodsinterval(({getExp(exp.Arguments[0])})*60,'second')";
					case "FromSeconds": return $"numtodsinterval(({getExp(exp.Arguments[0])}),'second')";
					case "FromTicks": return $"numtodsinterval(({getExp(exp.Arguments[0])})/10000000,'second')";
					case "Parse": return $"cast({getExp(exp.Arguments[0])} as interval day(9) to second(7))";
					case "ParseExact":
					case "TryParse":
					case "TryParseExact": return $"cast({getExp(exp.Arguments[0])} as interval day(9) to second(7))";
				}
			} else {
				var left = getExp(exp.Object);
				var args1 = exp.Arguments.Count == 0 ? null : getExp(exp.Arguments[0]);
				switch (exp.Method.Name) {
					case "Add": return $"({left}+{args1})";
					case "Subtract": return $"({left}-({args1}))";
					case "Equals": return $"({left} = {getExp(exp.Arguments[0])})";
					case "CompareTo": return $"extract(day from ({left}-({getExp(exp.Arguments[0])})))";
					case "ToString": return $"to_char({left})";
				}
			}
			throw new Exception($"OracleExpression 未现实函数表达式 {exp} 解析");
		}
		internal override string ExpressionLambdaToSqlCallConvert(MethodCallExpression exp, List<SelectTableInfo> _tables, List<SelectColumnInfo> _selectColumnMap, Func<Expression[], string> getSelectGroupingMapString, SelectTableInfoType tbtype, bool isQuoteName) {
			Func<Expression, string> getExp = exparg => ExpressionLambdaToSql(exparg, _tables, _selectColumnMap, getSelectGroupingMapString, tbtype, isQuoteName);
			if (exp.Object == null) {
				switch (exp.Method.Name) {
					//case "ToBoolean": return $"({getExp(exp.Arguments[0])} not in ('0','false'))";
					case "ToByte": return $"cast({getExp(exp.Arguments[0])} as number)";
					case "ToChar": return $"substr(to_char({getExp(exp.Arguments[0])}), 1, 1)";
					case "ToDateTime": return $"to_timestamp({getExp(exp.Arguments[0])},'YYYY-MM-DD HH24:MI:SS.FF6')";
					case "ToDecimal": return $"cast({getExp(exp.Arguments[0])} as number)";
					case "ToDouble": return $"cast({getExp(exp.Arguments[0])} as number)";
					case "ToInt16": 
					case "ToInt32": 
					case "ToInt64":
					case "ToSByte": return $"cast({getExp(exp.Arguments[0])} as number)";
					case "ToSingle": return $"cast({getExp(exp.Arguments[0])} as number)";
					case "ToString": return $"to_char({getExp(exp.Arguments[0])})";
					case "ToUInt16":
					case "ToUInt32":
					case "ToUInt64": return $"cast({getExp(exp.Arguments[0])} as number)";
				}
			}
			throw new Exception($"OracleExpression 未现实函数表达式 {exp} 解析");
		}
	}
}
