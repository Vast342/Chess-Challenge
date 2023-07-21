using ChessChallenge.API;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;

public class MyBot : IChessBot
{
    public Move Think(Board board, Timer timer)
    {
        var rng = new Random();
        List<Move> moves = board.GetLegalMoves().ToList();
        



        return moves[rng.Next(moves.Count-1)];
    }
}