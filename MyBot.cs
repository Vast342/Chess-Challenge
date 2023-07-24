using ChessChallenge.API;
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
    */
    public Move Think(Board board, Timer timer)
    {
        // Idea 3
        // randomizer if it can't decide on a move
        var rng = new Random();
        // lists of possible moves
        Move[] moves = board.GetLegalMoves();
        Move[] captures = board.GetLegalMoves(true);
        // these are used for the deeper search
        Move[] moreMoves;
        Move[] moreMoreMoves;
        //Move[] moreMoreMoreMoves;
        // a few bools for if a piece is defended or not and if a particular checkmate idea is preventable or not.
        bool attacked;
        bool preventable;
        // an index to be used for randomizer later
        int index = 0;
        // depth 1 search
        foreach(Move move in moves) {
            // it's funny
            if(move.IsEnPassant)return move;
            // detection for if a move is attacked
            attacked = board.SquareIsAttackedByOpponent(move.TargetSquare);
            // make the move
            board.MakeMove(move);
            // if mate in 1, DO IT
            if(board.IsInCheckmate()) {
                return move;
            }
            // depth 2 search
            moreMoves = board.GetLegalMoves();
            foreach(Move move1 in moreMoves) {
                // make the move
                board.MakeMove(move1);
                // if the opponent has mate in 1
                if(board.IsInCheckmate()) {
                    // PANIC
                    // goes back to searching the player's move
                    moreMoreMoves = board.GetLegalMoves();
                    board.UndoMove(move1);
                    board.UndoMove(move);
                    // try all of them until its not mate anymore
                    foreach(Move move2 in moves) {
                        board.MakeMove(move2);
                        board.MakeMove(move1);
                        if(!board.IsInCheckmate()) {
                            return move2;
                        }
                        board.UndoMove(move1);
                        board.UndoMove(move2);
                    }
                    // catch up
                    board.MakeMove(move);
                    board.MakeMove(move1);
                }
                // depth 3 search
                moreMoreMoves = board.GetLegalMoves();
                foreach(Move move2 in moreMoreMoves) {
                    // makes the move
                    board.MakeMove(move2);
                    // if mate in 2
                    if(board.IsInCheckmate()) {
                        // check if the mate is preventable
                        board.UndoMove(move2);
                        board.UndoMove(move1);
                        preventable = false;
                        foreach(Move move3 in moreMoves) {
                            board.MakeMove(move3);
                            board.MakeMove(move2);
                            if(!board.IsInCheckmate()) {
                                preventable = true;
                            }
                            board.UndoMove(move2);
                            board.UndoMove(move3);
                        }
                        // if not then do the checkmate
                        if(!preventable) {
                            return move;
                        }
                        board.MakeMove(move1);
                        board.MakeMove(move2);
                    }
                    // currently unimplemented depth 4 search
                    /*moreMoreMoreMoves = board.GetLegalMoves();
                    foreach (Move move3 in moreMoreMoreMoves) {
                        board.MakeMove(move3);
                        if(board.IsInCheckmate()) {
                            // PANIC
                        }
                        board.UndoMove(move3);
                    }
                    */
                    // undo the moves
                    board.UndoMove(move2);
                }
                board.UndoMove(move1);
            }
            // if not attacked check then go for it
            if(board.IsInCheck() && !attacked) {                
                return move;
            }
            board.UndoMove(move);     
        }
        // random capture (old code was skipping a buncha stuff)
        if(captures.Length > 0) {
            for(int i = 0; i < 1; i++) {
                index = rng.Next(0, captures.Length-1);
                if(board.SquareIsAttackedByOpponent(captures[index].TargetSquare)) {
                    i--;
                }
            }
            return captures[index];
        }
        // random move if its not under attack unless you are in check
        if(board.IsInCheck()) {
            return moves[0];
        } else {
        for(int i = 0; i < 1; i++) {
            index = rng.Next(0, moves.Length-1);
            if(board.SquareIsAttackedByOpponent(moves[index].TargetSquare)) {
                i--;
            }
        }
        return moves[index];
        }
    }
}