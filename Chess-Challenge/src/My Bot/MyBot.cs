using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using ChessChallenge.Application;
using System.Runtime.Intrinsics.X86;

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
    that I sorta made up, so I'm working on that now. The concept with this is to make an easy way to evaluate the board (I can make it more token efficient later) and then
    search and evaluate every possible move to a certain depth and then do the best move.
    Day 5: 7/25/23.
    And instead I forgot to work on that at all and was just making a search for checkmates. Oops. 
    Now I make an evaluations.
    Day 6: 7/26/23.
    I made a basic material evaluation, commented out at the bottom.
    And now its turned straight back into the search. i guess it's time to try negamax or something idk
    getting a bit bored and distracted and consistently nothing is working so AAAAAAAAAAAAAAAAAAAAAAAAAAA
    Day 7: 7/27/23.
    So I made some negamax and it works really well, except its terrible at openings and endgames so I'm going to try to make the evaluation better and prioritize piece
    positioning.
    I spent the entire day learning about Selenaut's code for compressing piece square tables and tried to modify that for use with PeSTO's piece-square tables and a rudimentary
    endgame detection, that ended up making it worse. I just had a great conversation with Ellie M about different aspects of chess bots so in total this is probably the most
    i've learned in a single day in years.
    Day 8: 7/28/23.
    So I decided to completely rewrite my negamax because I didn't understand it much. It worked better. So I remade my AB Pruning, and it worked faster. I made a Quiecense
    (probably not spelled right), search, and it immediately failed. So I did more research, and now it works. Up next, Transposition Tables and Iterative Deepening.
    */

    int[] pieceValues = {0, 100, 310, 330, 500, 1000, 50000};
    int universalDepth;
    int bigNumber = 999999999;

    Move selectedMove;
    public Move Think(Board board, Timer timer) {
        universalDepth = 3;
        NegaMax(universalDepth, board, board.IsWhiteToMove ? 1 : -1, -bigNumber, bigNumber);
        return selectedMove;
    }
    int Evaluate(Board board, int color) {
        if(board.IsDraw()) {
            return 0;
        }
        if(board.IsInCheckmate()) {
            return -1000000;
        }
        int sum = board.GetLegalMoves().Length;
        for(int i = 1; i < 7; i++) {
            sum += (board.GetPieceList((PieceType)i, true).Count - board.GetPieceList((PieceType)i, false).Count) * pieceValues[i];
        }
        return color * sum;
    }
    int NegaMax(int depth, Board board, int color, int alpha, int beta) {
        if(depth == 0) {
            return QSearch(board, color, alpha, beta);
            //return Evaluate(board, color);
        }
        foreach(Move move in board.GetLegalMoves()) {
            board.MakeMove(move);
            int score = -NegaMax(depth - 1, board, -color, -beta, -alpha);
            board.UndoMove(move);
            if(score > alpha) {
                alpha = score;
                if(depth == universalDepth) {
                    selectedMove = move;
                }
            }
            if(score >= beta) {
                return beta;
            }
        }
        return alpha;
    }
    int QSearch(Board board, int color, int alpha, int beta) {
        int stand_pat = Evaluate(board, color);
        if(stand_pat >= beta) {
            return beta;
        }
        if(alpha < stand_pat) {
            alpha = stand_pat;
        }
        Move[] moves = board.GetLegalMoves(true);
        OrderMoves(ref moves);
        foreach(Move move in moves) {
            board.MakeMove(move);
            int score = -QSearch(board, -color, -beta, -alpha);
            board.UndoMove(move);
            if(score >= beta) {
                return beta;
            }
            if(score > alpha) {
                alpha = score;
            }
        }
        return alpha;
    }

    public void OrderMoves(ref Move[] moves) {
        float[] scores = new float[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i].CapturePieceType != PieceType.None)
                scores[i] = 10 * pieceValues[(int)(moves[i].CapturePieceType)] - pieceValues[(int)(moves[i].MovePieceType)];
            else
                scores[i] = -99999999;

            scores[i] = -scores[i];
        }
        Array.Sort(scores, moves);
    }
}