using ChessChallenge.API;
using ChessChallenge.Application;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

public class MyBot : IChessBot
{
    /*
    LOG OF MY WORK
    Day 1: 7/21/23.
    I had a variety of strange ideas today, I worked mostly on an idea where it would search for both its and its opponents checkmates, then captures, then attacks, 
    and ONLY THEN would it do a random move. That worked until I cloned the repo and accidentally deleted it and began working on this evaluation idea instead. It's 
    not great so far though, and just makes random pawn walls for some reason. It's 12:52 AM right now though so I need sleep. (That section was typed on 7/22)
    Day 2: 7/22/23.
    While forgetting to sleep I had the great idea of storing information in a board and it's FEN string. If we ignore the gaps because it's painful to work with, we still
    have 12 unique pieces to work with, so we basically have 64 bits of base 11, or like 4.4579156845259056E+66 combinations which is more than enough to work with.
    And it turns out that that might not be useful. Each character in a string counts as a token, and a full fen string has 64 characters for pieces and 7 slashes, making it
    use 71 total tokens. That is about 1/15th of the total tokens, which is most likely not worth it. I was mostly busy today and didn't work much on it after that discovery.
    My next idea is going back to the checks captures attacks idea.
    Day 3: 7/23/23.
    Another not very productive day, I had to spend a lot of my time since I'm in the home stretch of my classes this summer. I've refined my idea a bit more with a search method
    that I sorta made up, so I'm working on that now. The concept with this is to make an easy way to evaluate the board (I can make it more token efficient later) and then search
    and evaluate every possible move to a certain depth and then do the best move.
    Day 5: 7/24/23.
    And instead I forgot to work on that at all and was just making a search for checkmates. Oops. 
    Now I make an evaluations.
    */
    public Move Think(Board board, Timer timer)
    {
        return null;
    }
}