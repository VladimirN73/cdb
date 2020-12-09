using System;
using System.Collections.Generic;
using System.Linq;

namespace MsSqlCloneDb.TextProcessor
{
    public class SimpleTextProcessor 
    {
        private string _beginTag = "#{";
        private string _endTag = "}#";
        private VariableProvider _variableProvider;

        public void Init(string beginTag, string endTag, VariableProvider variableProvider = null)
        {
            _beginTag = beginTag;
            _endTag = endTag;
            _variableProvider = variableProvider;
        }

        public void Init(VariableProvider variableProvider)
        {
            _variableProvider = variableProvider;
        }


        public string GetText(string strText)
        {
            if (strText.IsNullOrEmpty())
            {
                return strText;
            }

            if (_variableProvider == null)
            {
                throw new Exception($@"{nameof(_variableProvider)} is null");
            }

            int iPos = 0;
            int iCurrent = 0;
            var strOutput = "";

            while (iPos >= 0)
            {
                var strVariable = GetNextVariable(strText, iPos, out iPos);

                if (iPos >= 0)
                {
                    strOutput += strText.Substring(iCurrent, iPos - iCurrent);
                    var strVariableWithoutTags = strVariable
                        .ToUpper()
                        .Substring(_beginTag.Length, strVariable.Length - _beginTag.Length - _endTag.Length);

                    var str = _variableProvider.GetValue(strVariableWithoutTags, strVariable);

                    if (string.Compare(str, strVariable, StringComparison.InvariantCultureIgnoreCase) > 0)
                    {
                        strOutput += GetText(str);
                    }
                    else
                    {
                        strOutput += str;
                    }

                    iPos += strVariable.Length;
                    iCurrent = iPos;
                }
            }

            strOutput += strText.Substring(iCurrent, strText.Length - iCurrent);

            return strOutput;
        }

        protected string GetNextVariable(string strText, int startIndex, out int iPos)
        {
            iPos = -1;
            if (strText.IsNullOrEmpty() || startIndex >= strText.Length)
            {
                return "";
            }

            int i = strText.IndexOf(_beginTag, startIndex, StringComparison.InvariantCultureIgnoreCase);
            if (i < 0)
            {
                return "";
            }

            int j = strText.IndexOf(_endTag, i + _beginTag.Length, StringComparison.InvariantCultureIgnoreCase);
            if (j < 0)
            {
                return "";
            }

            var strOutput = strText.Substring(i, j - i + _endTag.Length);
            iPos = i;

            return strOutput;
        }
    }


    public class VariableProvider
    {
        public const string VAR_SOURCE_DB = @"SourceDB";
        public const string VAR_TARGET_DB = @"TargetDB";

        private readonly Dictionary<string, string> _variables = new Dictionary<string, string>();

        public virtual string GetValue(
            string variableName,  
            string defaultValue = null)
        {
            
            if (!_variables.ContainsKey(variableName))
            {
                return defaultValue;
            }

            var ret = _variables[variableName];

            return ret;
        }

        public void Add(string key, string value)
        {
            _variables.Add(key.Trim().ToUpper(), value);
        }

        public void PrintVariables(ILogSink logger)
        {
            var list = _variables.OrderBy(x => x.Key);

            logger.AddLogEntry(@"-- Print known Variables");

            foreach (var item in list)
            {
                logger.AddLogEntry($"{item.Key}:'{item.Value}'");
            }
        }

    }
}
