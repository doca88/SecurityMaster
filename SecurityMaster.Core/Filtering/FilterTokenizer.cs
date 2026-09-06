namespace SecurityMaster.Core.Filtering;

public enum TokenType
{
    Identifier, StringValue, NumberValue,
    And, Or, Equal, NotEqual, GreaterThan, LessThan, GreaterOrEqual, LessOrEqual,
    Contains, LParen, RParen, End
}

public record Token(TokenType Type, string Text);

public class FilterTokenizer
{
    private readonly string _input;
    private int _pos;

    public FilterTokenizer(string input) => _input = input;

    public List<Token> Tokenize()
    {
        var tokens = new List<Token>();
        while (_pos < _input.Length)
        {
            SkipWhitespace();
            if (_pos >= _input.Length) break;

            char c = _input[_pos];

            if (c == '(') { tokens.Add(new Token(TokenType.LParen, "(")); _pos++; continue; }
            if (c == ')') { tokens.Add(new Token(TokenType.RParen, ")")); _pos++; continue; }

            if (Match("&&")) { tokens.Add(new Token(TokenType.And, "&&")); continue; }
            if (Match("||")) { tokens.Add(new Token(TokenType.Or, "||")); continue; }
            if (Match(">=")) { tokens.Add(new Token(TokenType.GreaterOrEqual, ">=")); continue; }
            if (Match("<=")) { tokens.Add(new Token(TokenType.LessOrEqual, "<=")); continue; }
            if (Match("!=")) { tokens.Add(new Token(TokenType.NotEqual, "!=")); continue; }
            if (Match("==")) { tokens.Add(new Token(TokenType.Equal, "==")); continue; }
            if (Match("=")) { tokens.Add(new Token(TokenType.Equal, "=")); continue; }
            if (Match(">")) { tokens.Add(new Token(TokenType.GreaterThan, ">")); continue; }
            if (Match("<")) { tokens.Add(new Token(TokenType.LessThan, "<")); continue; }

            if (c == '"')
            {
                _pos++;
                var start = _pos;
                while (_pos < _input.Length && _input[_pos] != '"') _pos++;
                tokens.Add(new Token(TokenType.StringValue, _input[start.._pos]));
                _pos++; // skip closing quote
                continue;
            }

            if (char.IsDigit(c))
            {
                var start = _pos;
                while (_pos < _input.Length && (char.IsDigit(_input[_pos]) || _input[_pos] == '.')) _pos++;
                tokens.Add(new Token(TokenType.NumberValue, _input[start.._pos]));
                continue;
            }

            if (char.IsLetter(c) || c == '_')
            {
                var start = _pos;
                while (_pos < _input.Length && (char.IsLetterOrDigit(_input[_pos]) || _input[_pos] == '_' || _input[_pos] == '.')) _pos++;
                var word = _input[start.._pos];

                tokens.Add(word.Equals("contains", StringComparison.OrdinalIgnoreCase)
                    ? new Token(TokenType.Contains, word)
                    : new Token(TokenType.Identifier, word));
                continue;
            }

            throw new FormatException($"Unexpected character '{c}' at position {_pos}");
        }

        tokens.Add(new Token(TokenType.End, ""));
        return tokens;
    }

    private void SkipWhitespace() { while (_pos < _input.Length && char.IsWhiteSpace(_input[_pos])) _pos++; }

    private bool Match(string s)
    {
        if (_pos + s.Length > _input.Length) return false;
        if (_input.Substring(_pos, s.Length) != s) return false;
        _pos += s.Length;
        return true;
    }
}