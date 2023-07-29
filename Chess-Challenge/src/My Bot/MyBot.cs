using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using ChessChallenge.Application;
using System.Runtime.Intrinsics.X86;

public class MyBot : IChessBot
{
    int[] pieceValues = {0, 100, 310, 330, 500, 1000, 50000};
    int universalDepth;
    int bigNumber = 999999999;

    private const sbyte EXACT = 0, LOWERBOUND = -1, UPPERBOUND = 1, INVALID = -2;

    static ulong mask = 0x7FFFFF;
    Transposition[] TT = new Transposition[mask + 1];
    public struct Transposition {
        public Transposition(ulong zHash, Move decided, int d, int eval) {
            zobristHash = zHash;
            decidedMove = decided;
            depth = d;
            flag = INVALID;
            evaluation = eval;
        }

        public ulong zobristHash = 0;
        public Move decidedMove = Move.NullMove;
        public int evaluation = 0;
        public int depth = 0;
        public sbyte flag = INVALID;
    }; 
    int startingPlyCount;
    Move selectedMove;
    public Move Think(Board board, Timer timer) {
        int depthToUse = BitOperations.PopCount(board.AllPiecesBitboard) < 8 ? 6 : 4;
        startingPlyCount = board.PlyCount;
        // ADD THE PIECE COUNT ENDGAME DETECTION HERE, INCREASE DEPTH TO LIKE 7 SINCE THERE ARE SO FEW MOVES TO CALCULATE
        for(int i = 1; i < depthToUse; i++) {
            universalDepth = i;
            NegaMax(universalDepth, board, -bigNumber, bigNumber);
            board.MakeMove(selectedMove);
            if(board.IsInCheckmate()) {
                return selectedMove;
            }
            board.UndoMove(selectedMove);
        }
        return selectedMove;
    }
    int Evaluate(Board board) {
        if(board.IsDraw()) {
            return 0;
        }
        if(board.IsInCheckmate()) {
            return -1000000 + board.PlyCount;
        }
        int sum = board.GetLegalMoves().Length;
        for(int i = 1; i < 7; i++) {
            sum += (board.GetPieceList((PieceType)i, true).Count - board.GetPieceList((PieceType)i, false).Count) * pieceValues[i];
        }
        if(board.IsInCheck()) sum+=100;
        return board.IsWhiteToMove ? sum : -sum;
    }
    int NegaMax(int depth, Board board, int alpha, int beta) {
        if(board.IsInCheckmate()) {
            return -1000000 + board.PlyCount;
        }
        if(board.IsDraw()) {
            return 0;
        }
        if(depth == 0) {
            return QSearch(board, alpha, beta, depth);
        }
        Move[] moves = board.GetLegalMoves();
        OrderMoves(ref moves);
        foreach(Move move in moves) {
            board.MakeMove(move);
            int score = -NegaMax(depth - 1, board, -beta, -alpha);
            TT[board.ZobristKey & mask] = new Transposition(board.ZobristKey, move, depth-1, score);
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
    int QSearch(Board board, int alpha, int beta, int depth) {
        if(board.IsInCheckmate()) {
            return -1000000 + board.PlyCount;
        }
        if(board.IsDraw()) {
            return 0;
        }
        int stand_pat = Evaluate(board);
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
            int score = -QSearch(board, -beta, -alpha, depth);
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
                scores[i] = 10 * pieceValues[(int)moves[i].CapturePieceType] - pieceValues[(int)moves[i].MovePieceType];
            else
                scores[i] = -bigNumber;

            scores[i] = -scores[i];
        }
        Array.Sort(scores, moves);
    }

    int TableLookup(Board board, int depth) {
        if(board.IsInCheckmate()) {
            return -1000000 + board.PlyCount;
        }
        if(board.IsDraw()) {
            return 0;
        }
        ulong zkey = board.ZobristKey;
        if(TT[zkey & mask].zobristHash == zkey) {
            if(TT[zkey & mask].depth >= depth) {
                return TT[zkey & mask].evaluation;
            }
        } else {
            TT[zkey & mask] = new Transposition(zkey, selectedMove, depth, Evaluate(board));
        }
        return TT[zkey & mask].evaluation;
    }
}