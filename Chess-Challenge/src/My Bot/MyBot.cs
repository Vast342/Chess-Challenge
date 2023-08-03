using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections.Generic;
using ChessChallenge.Application;

public class MyBot : IChessBot
{
    int[] pieceValues = {0, 100, 310, 330, 500, 1000, 10000};
    int[] piecePhase = {0, 0, 1, 1, 2, 4, 0};
    ulong[] psts = {657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902};

    int startingPly;

    int universalDepth = 0;

    bool timesUp = false;

    static ulong mask = 0x7FFFFF;

    Transposition[] TT = new Transposition[mask + 1];

    // no flag yet
    // Raoul says: If the depth in the TT >= to your current depth, then you can use it to update alpha and beta. And potentially retrieve an exact value or find a beta cutoff
    //0u:Exact,         this is the solution, regardless of alpha and beta. 
    //1u:LowerBound,    My eval will be greater or equal to this. 
    //2u:Upperbound,    My eval will be smaller or equal to this.
    public struct Transposition {
        public Transposition(ulong zHash, int d, int eval, Move move, int c) {
            zobristHash = zHash;
            depth = d;
            evaluation = eval;
            bestMove = move;
            flag = -1;
            cutoffs = c;
        }

        public ulong zobristHash = 0;
        public int evaluation = 0, depth = 0, flag = -1, cutoffs = 0;
        public Move bestMove;
    }; 
    Move lastChosenMove;
    public Move Think(Board board, Timer timer) {
        bool searched = false;
        timesUp = false;
        for(int depth = 1; depth <= 10; depth++) {
            startingPly = board.PlyCount;
            universalDepth = depth;
            lastChosenMove = TT[board.ZobristKey & mask].bestMove;
            NegaMax(depth, board, -2147483646, 2147483646, timer, false, false);
            if(timesUp) {
                if(!board.IsInCheck()) {
                    if(depth == 1) return board.GetLegalMoves()[0];
                    return lastChosenMove;   
                } else {
                    if(searched == false) {
                        searched = true;
                    } else {
                        break;
                    }
                }
            }
        }
        return TT[board.ZobristKey & mask].bestMove;
    }
    int GetPieceBonus(int index)
        => (int)(((psts[index / 10] >> (6 * (index % 10))) & 63) - 20) * 8;
    int Evaluate(Board board) {
        int mg = board.GetLegalMoves().Length, eg = mg, phase = 0;
        if(mg == 0) {
            if(board.IsInCheck()) return -10000000 + board.PlyCount;
            return 0;
        }
        foreach(bool color in new[] {true, false}) {
            for(int p = 0; p < 7; p++) {
                ulong mask = board.GetPieceBitboard((PieceType)p, color);
                while(mask != 0) {
                    phase += piecePhase[p];
                    int ind = 128 * (p - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (color ? 56 : 0);
                    mg += GetPieceBonus(ind) + pieceValues[p];
                    eg += GetPieceBonus(ind + 64) + pieceValues[p];
                }
            }
            mg = -mg;
            eg = -eg;
        }
        return (mg * phase + eg * (24 - phase) / 24) * (board.IsWhiteToMove ? 1 : -1);
    }
    int NegaMax(int depth, Board board, int alpha, int beta, Timer timer, bool qSearch, bool gotNMP) {
        if(timesUp) return 0;
        int startAlpha = alpha;
        if(board.IsDraw()) return 0;
        if(board.IsInCheckmate()) return -100000000 + board.PlyCount - startingPly;
        var entry = TT[board.ZobristKey & mask];
        if(startingPly != board.PlyCount && entry.zobristHash == board.ZobristKey && entry.depth >= depth && entry.flag != -1) return entry.evaluation;
        if(entry.zobristHash != board.ZobristKey) entry.zobristHash = board.ZobristKey;
        if(depth == 0 && !qSearch) { 
            int score3 = NegaMax(0, board, alpha, beta, timer, true, gotNMP);
            entry.evaluation = score3;
            entry.flag = 0;
            return score3;
        }
        if(qSearch) {
            int stand_pat = Evaluate(board);
            if(stand_pat >= beta) return beta;
            if(alpha < stand_pat) alpha = stand_pat;
        }
        if(!board.IsInCheck() && depth > 2 && gotNMP == false) {
            board.MakeMove(Move.NullMove);
            int score2 = -NegaMax(qSearch ? 0 : depth - 3, board, -beta, -alpha, timer, qSearch, true);
            board.UndoMove(Move.NullMove);
            if(score2 >= beta) return beta;
        }
        var moves = board.GetLegalMoves(qSearch);
        OrderMoves(ref moves, board, depth);
        foreach(Move move in moves) {
            timesUp = timer.MillisecondsElapsedThisTurn >= timer.MillisecondsRemaining / 30;
            if(timesUp) return 0;
            board.MakeMove(move);
            int score = -NegaMax(qSearch ? 0 : depth - 1, board, -beta, -alpha, timer, qSearch, gotNMP);
            board.UndoMove(move);
            if(score > alpha) {
                TT[board.ZobristKey & mask].bestMove = move;
                alpha = score;
                TT[board.ZobristKey & mask].cutoffs++;
            }
            if(score >= beta) {
                TT[board.ZobristKey & mask].evaluation = beta;
                TT[board.ZobristKey & mask].flag = 2;
                TT[board.ZobristKey & mask].depth = depth;
                TT[board.ZobristKey & mask].cutoffs++;
                return beta;
            }
        }
        TT[board.ZobristKey & mask].depth = depth;
        TT[board.ZobristKey & mask].evaluation = alpha;
        if(alpha == startAlpha) {
            TT[board.ZobristKey & mask].flag = 1;
        } else {
            TT[board.ZobristKey & mask].flag = 0;
        }
        return alpha;
    }

    public void OrderMoves(ref Move[] moves, Board board, int depth) {
        var scores = new float[moves.Length];
        for (int i = 0; i < moves.Length; i++) {
            if (moves[i].CapturePieceType != PieceType.None) scores[i] += pieceValues[(int)moves[i].CapturePieceType] - pieceValues[(int)moves[i].MovePieceType]; // mvv lva
            if(TT[board.ZobristKey & mask].bestMove == moves[i]) scores[i] += 10000000; // if the move is found earlier then bias it more in the search
            scores[i] += TT[board.ZobristKey & mask].cutoffs * depth^2;
        }
        Array.Sort(scores, moves);
        Array.Reverse(moves);
    }
}