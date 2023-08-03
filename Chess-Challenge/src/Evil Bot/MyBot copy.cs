using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;

public class Evi3lBot : IChessBot
{
    int[] pieceValues = {0, 100, 300, 310, 500, 1000, 50000};
    int universalDepth;
    int bigNumber = 2147483647;

    static ulong mask = 0x7FFFFF;

    Transposition[] TT = new Transposition[mask + 1];

    public struct Transposition {
        public Transposition(ulong zHash, int d, int eval) {
            zobristHash = zHash;
            depth = d;
            evaluation = eval;
        }

        public ulong zobristHash = 0;
        public int evaluation = 0;
        public int depth = 0;
    }; 


    int depthToUse = 0;
    Move selectedMove;
    public Move Think(Board board, Timer timer) {
        depthToUse = BitOperations.PopCount(board.AllPiecesBitboard) < 8 ? 7 : 4;
        if(board.IsInCheck()) depthToUse++;
        // ADD THE PIECE COUNT ENDGAME DETECTION HERE, INCREASE DEPTH TO LIKE 7 SINCE THERE ARE SO FEW MOVES TO CALCULATE
        for(int i = 1; i < depthToUse; i++) {
            universalDepth = i;
            NegaMax(universalDepth, board, -bigNumber, bigNumber);
        }
        board.MakeMove(selectedMove);
        return selectedMove;
    }
    int Evaluate(Board board) {
        if(board.IsDraw()) return 0;
        if(board.IsInCheckmate()) return -100000000 + board.PlyCount;
        int sum = board.GetLegalMoves().Length;
        for(int i = 1; i < 6; i++) {
            sum += (board.GetPieceList((PieceType)i, true).Count - board.GetPieceList((PieceType)i, false).Count) * pieceValues[i];
        }
        if(board.IsInCheck()) sum += 50;
        return board.IsWhiteToMove ? sum : -sum;
    }
    int NegaMax(int depth, Board board, int alpha, int beta) {
        if(board.IsDraw()) return 0;
        if(board.IsInCheckmate()) return -100000000 + board.PlyCount;
        if(depth == 0) {
            return QSearch(board, alpha, beta, depth);
        }
        Move[] moves = board.GetLegalMoves();
        OrderMoves(ref moves);
        foreach(Move move in moves) {
            board.MakeMove(move);
            int score = -NegaMax(depth - 1, board, -beta, -alpha);
            TT[board.ZobristKey & mask] = new Transposition(board.ZobristKey, depth-1, score);
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
        int stand_pat = TableLookup(board, depth);
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
        ulong zkey = board.ZobristKey;
        if(TT[zkey & mask].zobristHash != zkey) {
            TT[zkey & mask] = new Transposition(zkey, depth, Evaluate(board));
        }
        return TT[zkey & mask].evaluation;
    }
}