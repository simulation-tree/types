﻿using System.Collections.Generic;
using System.Text;

namespace Types
{
    public class SourceBuilder
    {
        private readonly StringBuilder builder = new();
        private int indentation;
        private bool needsToIndent;

        public IEnumerable<string> Lines
        {
            get
            {
                string[] lines = builder.ToString().Split('\n');
                for (int i = 0; i < lines.Length; i++)
                {
                    yield return lines[i].TrimEnd('\r');
                }
            }
        }

        public int Length
        {
            get => builder.Length;
            set => builder.Length = value;
        }

        public override string ToString()
        {
            return builder.ToString();
        }

        public void Clear()
        {
            indentation = 0;
            builder.Clear();
            needsToIndent = false;
        }

        public void BeginGroup()
        {
            AppendIndentation();
            builder.Append('{');
            AppendLine();
            indentation++;
        }

        public void EndGroup()
        {
            indentation--;
            AppendIndentation();
            builder.Append('}');
            AppendLine();
        }

        public void AppendIndentation()
        {
            for (int i = 0; i < indentation; i++)
            {
                builder.Append("    ");
            }
        }

        public void AppendLine(object text)
        {
            AppendIndentation();
            builder.Append(text.ToString());
            AppendLine();
        }

        public void AppendLine(string text)
        {
            AppendIndentation();
            builder.Append(text);
            AppendLine();
        }

        public void AppendLine()
        {
            builder.AppendLine();
            needsToIndent = true;
        }

        public void Append(object text)
        {
            if (needsToIndent)
            {
                needsToIndent = false;
                AppendIndentation();
            }

            builder.Append(text.ToString());
        }

        public void Append(string text)
        {
            if (needsToIndent)
            {
                needsToIndent = false;
                AppendIndentation();
            }

            builder.Append(text);
        }

        public void Append(char character)
        {
            if (needsToIndent)
            {
                needsToIndent = false;
                AppendIndentation();
            }

            builder.Append(character);
        }
    }
}