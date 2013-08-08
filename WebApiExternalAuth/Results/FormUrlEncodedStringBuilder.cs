using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WebApiExternalAuth.Results
{
    public class FormUrlEncodedStringBuilder
    {
        private readonly StringBuilder _stringBuilder = new StringBuilder();

        private bool first = true;

        public void Add(string key, string value)
        {
            if (!first)
            {
                _stringBuilder.Append("&");
            }

            _stringBuilder.Append(Uri.EscapeDataString(key));
            _stringBuilder.Append("=");
            _stringBuilder.Append(Uri.EscapeDataString(value));

            first = false;
        }

        public string Build()
        {
            return _stringBuilder.ToString();
        }
    }
}
