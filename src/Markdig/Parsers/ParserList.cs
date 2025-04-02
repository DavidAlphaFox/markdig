// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Runtime.CompilerServices;

using Markdig.Helpers;

namespace Markdig.Parsers;

/// <summary>
/// Base class for a list of parsers.
/// </summary>
/// <typeparam name="T">Type of the parser</typeparam>
/// <typeparam name="TState">The type of the parser state.</typeparam>
/// <seealso cref="OrderedList{T}" />
public abstract class ParserList<T, TState> : OrderedList<T> where T : notnull, ParserBase<TState>
{
    //ParserList是OrderedList的子类
    private readonly CharacterMap<T[]> charMap;
    private readonly T[]? globalParsers;

    protected ParserList(IEnumerable<T> parsersArg) : base(parsersArg)
    {
        var charCounter = new Dictionary<char, int>();
        int globalCounter = 0;
        //遍历所有的parser，进行初始化，并标记每个parser所在的位置
        for (int i = 0; i < Count; i++)
        {
            var parser = this[i];
            if (parser is null)
            {
                ThrowHelper.InvalidOperationException("Unexpected null parser found");
            }
            //parser进行初始化，让parser记住自己的位置
            parser.Initialize();
            parser.Index = i;
            // C# 的属性模式
            //https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/operators/patterns#property-pattern
            if (parser.OpeningCharacters is { Length: > 0 })
            {   //记录每个指令开头字符所对应的Parsers数量
                foreach (var openingChar in parser.OpeningCharacters)
                {
                    if (!charCounter.ContainsKey(openingChar))
                    {
                        charCounter[openingChar] = 0;
                    }
                    charCounter[openingChar]++;
                }
            }
            else
            {
                globalCounter++; //如果没有指令开头字符，就认为是全局Parser
            }
        }
        // 存储全局Parser
        if (globalCounter > 0)
        {
            globalParsers = new T[globalCounter];
        }
        //再次便利Parsers,记录指令开头字符
        var tempCharMap = new Dictionary<char, T[]>();
        foreach (var parser in this)
        {
            if (parser.OpeningCharacters is { Length: > 0 })
            {
                foreach (var openingChar in parser.OpeningCharacters)
                {
                    if (!tempCharMap.TryGetValue(openingChar, out T[]? parsers))
                    {
                        parsers = new T[charCounter[openingChar]];
                        tempCharMap[openingChar] = parsers;
                    }
                    //将Parser放到对应指令开头字符所在的Parser集合上
                    var index = parsers.Length - charCounter[openingChar];
                    parsers[index] = parser;
                    charCounter[openingChar]--;
                }
            }
            else
            {
                globalParsers![globalParsers.Length - globalCounter] = parser;
                globalCounter--;
            }
        }
        //构建出开头指令字符所对应Parser的映射表
        charMap = new CharacterMap<T[]>(tempCharMap);
    }

    /// <summary>
    /// Gets the list of global parsers (that don't have any opening characters defined)
    /// </summary>
    public T[]? GlobalParsers => globalParsers;

    /// <summary>
    /// Gets all the opening characters defined.
    /// </summary>
    public char[] OpeningCharacters => charMap.OpeningCharacters;

    /// <summary>
    /// Gets the list of parsers valid for the specified opening character.
    /// </summary>
    /// <param name="openingChar">The opening character.</param>
    /// <returns>A list of parsers valid for the specified opening character or null if no parsers registered.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public T[]? GetParsersForOpeningCharacter(uint openingChar)
    {
        return charMap[openingChar];
    }

    /// <summary>
    /// Searches for an opening character from a registered parser in the specified string.
    /// </summary>
    /// <param name="text">The text.</param>
    /// <param name="start">The start.</param>
    /// <param name="end">The end.</param>
    /// <returns>Index position within the string of the first opening character found in the specified text; if not found, returns -1</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int IndexOfOpeningCharacter(string text, int start, int end)
    {
        return charMap.IndexOfOpeningCharacter(text, start, end);
    }
}