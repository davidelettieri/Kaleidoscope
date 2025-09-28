namespace Kaleidoscope.Shared;

public enum TokenType
{
    EOF,
    DEF,
    EXTERN,
    IDENTIFIER,
    NUMBER,
    IF,
    THEN,
    ELSE,
    FOR,
    IN,
    RIGHT_PAREN,
    LEFT_PAREN,
    COMMA,
    SEMICOLON,
    UNARY,
    BINARY,
    // types below are used only in Chapter 7
    VAR,
    EQUAL,
    EQUAL_EQUAL,
    PLUS,
    MINUS,
    LESS,
    STAR
}
