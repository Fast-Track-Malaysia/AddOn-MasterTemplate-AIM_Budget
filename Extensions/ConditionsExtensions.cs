using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FT_ADDON
{
    static class ConditionsExtensions
    {
        static readonly Regex rgx = new Regex("(\\(|\\)|\\<\\>|\\<=|\\>=|\\=|\\>|\\<|[a-zA-Z]+|'.*?'|[0-9]+|(?i)is null|is not null|or|and|not between|between)");
        static readonly Regex alphanumeric_rgx = new Regex("[^a-zA-Z0-9]+$");

        const string syntaxerror = "Choose from list expression syntax error";

        enum Lexer
        {
            Operator,
            Symbol,
            Relation,
        }

        public static bool TryParseExpression(this SAPbouiCOM.Conditions conditions, string expression)
        {
            var matches = rgx.Matches(expression);

            if (matches.Count == 0) return false;

            try
            {
                var condition = conditions.Add();
                condition.BracketOpenNum = 0;
                condition.BracketCloseNum = 0;
                Lexer nxt = Lexer.Symbol;

                foreach (var match in matches)
                {
                    if (nxt == Lexer.Symbol)
                    {
                        nxt = ParseSymbol(condition, match.ToString());
                        continue;
                    }
                    else if (nxt == Lexer.Operator)
                    {
                        nxt = ParseOperator(condition, match.ToString());
                        continue;
                    }

                    nxt = ParseRelation(condition, match.ToString());

                    if (nxt != Lexer.Symbol) continue;

                    condition = conditions.Add();
                }

                return true;
            }
            catch (Exception)
            {
                throw new MessageException("Invalid choose from list expression");
            }
        }

        static Lexer ParseRelation(this SAPbouiCOM.Condition condition, string value)
        {
            switch (value.ToLower())
            {
                case ")":
                    if (condition.Operation != SAPbouiCOM.BoConditionOperation.co_NONE) throw new MessageException(syntaxerror);

                    condition.BracketCloseNum++;
                    return Lexer.Relation;
                case "and":
                    if (condition.Operation == SAPbouiCOM.BoConditionOperation.co_BETWEEN || condition.Operation == SAPbouiCOM.BoConditionOperation.co_NOT_BETWEEN)
                    {
                        if (String.IsNullOrEmpty(condition.CondEndVal)) return Lexer.Symbol;
                    }

                    condition.Relationship = SAPbouiCOM.BoConditionRelationship.cr_AND;
                    break;
                case "or":
                    condition.Relationship = SAPbouiCOM.BoConditionRelationship.cr_OR;
                    break;
                default:
                    throw new MessageException(syntaxerror);
            }

            return Lexer.Symbol;
        }

        static Lexer ParseSymbol(this SAPbouiCOM.Condition condition, string value)
        {
            switch (value)
            {
                case "(":
                    if (condition.Operation != SAPbouiCOM.BoConditionOperation.co_NONE) throw new MessageException(syntaxerror);

                    condition.BracketOpenNum++;
                    return Lexer.Symbol;
                default:
                    if ((value.StartsWith("'") && value.EndsWith("'")) || double.TryParse(value, out var dv))
                    {
                        if (String.IsNullOrEmpty(condition.CondVal))
                        {
                            condition.CondVal = value;

                            return condition.Operation == SAPbouiCOM.BoConditionOperation.co_BETWEEN || condition.Operation == SAPbouiCOM.BoConditionOperation.co_NOT_BETWEEN ?
                                   Lexer.Relation :
                                   Lexer.Operator;
                        }
                        else if (condition.Operation == SAPbouiCOM.BoConditionOperation.co_BETWEEN || condition.Operation == SAPbouiCOM.BoConditionOperation.co_NOT_BETWEEN)
                        {
                            condition.CondEndVal = value;
                            return Lexer.Operator;
                        }

                        throw new MessageException(syntaxerror);
                    }

                    if (!alphanumeric_rgx.IsMatch(value)) throw new MessageException(syntaxerror);

                    if (String.IsNullOrEmpty(condition.Alias))
                    {
                        condition.Alias = value;

                        return condition.Operation != SAPbouiCOM.BoConditionOperation.co_NONE ? 
                               Lexer.Relation : 
                               Lexer.Operator;
                    }

                    if (condition.Operation == SAPbouiCOM.BoConditionOperation.co_NONE) throw new MessageException(syntaxerror);

                    condition.ComparedAlias = value;
                    return Lexer.Relation;
            }
        }

        static Lexer ParseOperator(this SAPbouiCOM.Condition condition, string operatorstr)
        {
            if (String.IsNullOrEmpty(condition.Alias) && String.IsNullOrEmpty(condition.CondVal)) throw new MessageException(syntaxerror);

            switch (operatorstr.ToLower())
            {
                case "=":
                    condition.Operation = SAPbouiCOM.BoConditionOperation.co_EQUAL;
                    break;
                case "<>":
                    condition.Operation = SAPbouiCOM.BoConditionOperation.co_NOT_EQUAL;
                    break;
                case ">=":
                    condition.Operation = String.IsNullOrEmpty(condition.Alias) ? SAPbouiCOM.BoConditionOperation.co_LESS_THAN : SAPbouiCOM.BoConditionOperation.co_GRATER_EQUAL;
                    break;
                case ">":
                    condition.Operation = String.IsNullOrEmpty(condition.Alias) ? SAPbouiCOM.BoConditionOperation.co_LESS_EQUAL : SAPbouiCOM.BoConditionOperation.co_GRATER_THAN;
                    break;
                case "<=":
                    condition.Operation = String.IsNullOrEmpty(condition.Alias) ? SAPbouiCOM.BoConditionOperation.co_GRATER_THAN : SAPbouiCOM.BoConditionOperation.co_LESS_EQUAL;
                    break;
                case "<":
                    condition.Operation = String.IsNullOrEmpty(condition.Alias) ? SAPbouiCOM.BoConditionOperation.co_GRATER_EQUAL : SAPbouiCOM.BoConditionOperation.co_LESS_THAN;
                    break;
                case "not between":
                    condition.Operation = SAPbouiCOM.BoConditionOperation.co_NOT_BETWEEN;
                    break;
                case "between":
                    condition.Operation = SAPbouiCOM.BoConditionOperation.co_BETWEEN;
                    break;
                default:
                    throw new MessageException(syntaxerror);
            }

            return Lexer.Symbol;
        }
    }
}
