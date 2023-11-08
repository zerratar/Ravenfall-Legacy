﻿namespace SqlParser.Ast
{

    /// <summary>
    /// Information about [character length][1], including length and possibly unit.
    ///
    /// <see href="https://jakewheat.github.io/sql-overview/sql-2016-foundation-grammar.html#character-length"/> 
    /// </summary>
    /// <param name="Length">Default (if VARYING) or maximum (if not VARYING) length</param>
    /// <param name="Unit">Optional unit. If not informed, the ANSI handles it as CHARACTERS implicitly</param>
    public class CharacterLength
    {
        public CharacterLength(ulong Length, CharLengthUnit Unit = CharLengthUnit.None)
        {
            this.Length = Length;
            this.Unit = Unit;
        }

        public ulong Length { get; set; }
        public CharLengthUnit Unit { get; set; }
    }
}