 using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using ChessChallenge.Application;

public class MyBoston : IChessBot
{
    int[] pieceValues = {0, 100, 310, 330, 500, 1000, 10000};
    int[] piecePhase = {0, 0, 1, 1, 2, 4, 0};
    int universalDepth;
    int bigNumber = 2147483647;
    ulong[] psts = {657614902731556116, 420894446315227099, 384592972471695068, 312245244820264086, 364876803783607569, 366006824779723922, 366006826859316500, 786039115310605588, 421220596516513823, 366011295806342421, 366006826859316436, 366006896669578452, 162218943720801556, 440575073001255824, 657087419459913430, 402634039558223453, 347425219986941203, 365698755348489557, 311382605788951956, 147850316371514514, 329107007234708689, 402598430990222677, 402611905376114006, 329415149680141460, 257053881053295759, 291134268204721362, 492947507967247313, 367159395376767958, 384021229732455700, 384307098409076181, 402035762391246293, 328847661003244824, 365712019230110867, 366002427738801364, 384307168185238804, 347996828560606484, 329692156834174227, 365439338182165780, 386018218798040211, 456959123538409047, 347157285952386452, 365711880701965780, 365997890021704981, 221896035722130452, 384289231362147538, 384307167128540502, 366006826859320596, 366006826876093716, 366002360093332756, 366006824694793492, 347992428333053139, 457508666683233428, 329723156783776785, 329401687190893908, 366002356855326100, 366288301819245844, 329978030930875600, 420621693221156179, 422042614449657239, 384602117564867863, 419505151144195476, 366274972473194070, 329406075454444949, 275354286769374224, 366855645423297932, 329991151972070674, 311105941360174354, 256772197720318995, 365993560693875923, 258219435335676691, 383730812414424149, 384601907111998612, 401758895947998613, 420612834953622999, 402607438610388375, 329978099633296596, 67159620133902};


    static ulong mask = 0x7FFFFF;

    Transposition[] TT = new Transposition[mask + 1];

    public struct Transposition {
        public Transposition(ulong zHash, int d, int eval, Move move) {
            zobristHash = zHash;
            depth = d;
            evaluation = eval;
            bestMove = move;
        }

        public ulong zobristHash = 0;
        public int evaluation = 0;
        public int depth = 0;
        public Move bestMove;
    }; 
    Move lastChosenMove;
    Move selectedMove;
    int startTime;
    public Move Think(Board board, Timer timer) {
        startTime = timer.MillisecondsRemaining;
        for(int depth = 1; depth <= 10; depth++) {
            universalDepth = depth;
            lastChosenMove = selectedMove;
            NegaMax(depth, board, -bigNumber, bigNumber, timer);
            if(timer.MillisecondsElapsedThisTurn >= startTime / 30)  {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Hit Time Limit of 1/30th in " + timer.MillisecondsElapsedThisTurn + " ms with a depth of " + depth);
                Console.ResetColor();
                if(depth == 1) return board.GetLegalMoves()[0];
                return lastChosenMove;   
            }
        }
        Console.ForegroundColor = ConsoleColor.Green;
        Console.WriteLine("Hit Depth Limit of 10 in " + timer.MillisecondsElapsedThisTurn + " ms");
        Console.ResetColor();
        return selectedMove;
    }
    int GetPieceBonus(int index) {
        return (int)(((psts[index / 10] >> (6 * (index % 10))) & 63) - 20) * 8;
    }
    int Evaluate(Board board) {
        if(board.IsDraw()) return 0;
        if(board.IsInCheckmate()) return -100000000 + board.PlyCount;
        int mg = 0, eg = 0, phase = 0;

        foreach(bool color in new[] {true, false}) {
            for(var p = PieceType.Pawn; p <= PieceType.King; p++) {
                int piece = (int)p, ind;
                ulong mask = board.GetPieceBitboard(p, color);
                while(mask != 0) {
                    phase += piecePhase[piece];
                    ind = 128 * (piece - 1) + BitboardHelper.ClearAndGetIndexOfLSB(ref mask) ^ (color ? 56 : 0);
                    mg += GetPieceBonus(ind) + pieceValues[piece];
                    eg += GetPieceBonus(ind + 64) + pieceValues[piece];
                }
            }

            mg = -mg;
            eg = -eg;
        }
        Console.WriteLine("Game Phase: " + phase + "/255");
        return (mg * phase + eg * (24 - phase) / 24) * (board.IsWhiteToMove ? 1 : -1);
    }
    int NegaMax(int depth, Board board, int alpha, int beta, Timer timer) {
        Console.WriteLine("Evaluating " + board.GetFenString());
        if(board.IsDraw()) return 0;
        if(board.IsInCheckmate()) return -100000000 + board.PlyCount;
        if(depth == 0) {
            Console.WriteLine("At depth 0, Start Q Search");
            return QSearch(board, alpha, beta, depth, timer);
        }
        Move[] moves = board.GetLegalMoves();
        OrderMoves(ref moves, board);
        foreach(Move move in moves) {
            Console.WriteLine("Evaluating " + move);
            if(timer.MillisecondsElapsedThisTurn >= startTime / 30) return 30000;
            board.MakeMove(move);
            int score = -NegaMax(depth - 1, board, -beta, -alpha, timer);
            Console.WriteLine(move + " has a score of " + score);
            TT[board.ZobristKey & mask].zobristHash = board.ZobristKey;
            TT[board.ZobristKey & mask].depth = depth - 1;
            TT[board.ZobristKey & mask].evaluation = TableLookup(board, depth - 1);
            board.UndoMove(move);
            if(score > alpha) {
                Console.WriteLine("New best move: " + move + " At " + board.GetFenString());
                alpha = score;
                TT[board.ZobristKey & mask].bestMove = move;
                if(depth == universalDepth) {
                    Console.ForegroundColor = ConsoleColor.Magenta;
                    Console.WriteLine("Move updated to: " + move);
                    Console.ResetColor();
                    selectedMove = move;
                }
            }
            if(score >= beta) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Fail-Hard, returning beta");
                Console.ResetColor();
                return beta;

            }
        }
        return alpha;
    }

    int QSearch(Board board, int alpha, int beta, int depth, Timer timer) {
        int stand_pat = TableLookup(board, depth);
        if(stand_pat >= beta) {
            return beta;
        }
        if(alpha < stand_pat) {
            alpha = stand_pat;
        }
        Move[] moves = board.GetLegalMoves(true);
        OrderMoves(ref moves, board);
        foreach(Move move in moves) {
            if(timer.MillisecondsElapsedThisTurn >= startTime / 30) return 30000;
            board.MakeMove(move);
            int score = -QSearch(board, -beta, -alpha, depth, timer);
            TT[board.ZobristKey & mask].zobristHash = board.ZobristKey;
            TT[board.ZobristKey & mask].depth = 0;
            TT[board.ZobristKey & mask].evaluation = TableLookup(board, depth - 1);
            board.UndoMove(move);
            if(score >= beta) {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Qsearch Fail-Hard, returning beta");
                Console.ResetColor();
                return beta;
            }
            if(score > alpha) {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("Qsearch New best move! " + move);
                Console.ResetColor();
                TT[board.ZobristKey & mask].bestMove = move;
                alpha = score;
            }
        }
        return alpha;
    }

    public void OrderMoves(ref Move[] moves, Board board) {
        float[] scores = new float[moves.Length];
        for (int i = 0; i < moves.Length; i++)
        {
            if (moves[i].CapturePieceType != PieceType.None)
                scores[i] += 10 * pieceValues[(int)moves[i].CapturePieceType] - pieceValues[(int)moves[i].MovePieceType];
            else
                scores[i] -= 10000000;
            if(TT[board.ZobristKey & mask].bestMove == moves[i]) {
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("Biased previous best move");
                Console.ResetColor();
                scores[i] += 10000000;
            }
            scores[i] = -scores[i];
        }
        Array.Sort(scores, moves);
    }

    int TableLookup(Board board, int depth) {
        ulong zkey = board.ZobristKey;
        if(TT[zkey & mask].zobristHash != zkey) {
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Evaluated Fresh, added " + board.ZobristKey + " to the TT");
            Console.ResetColor();
            TT[zkey & mask].zobristHash = zkey;
            TT[zkey & mask].depth = depth;
            TT[zkey & mask].evaluation = Evaluate(board);
        } else {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("Got Eval From TT At Position " + board.ZobristKey);
            Console.ResetColor();
        }
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.WriteLine(TT[zkey & mask].evaluation + " With FEN: " + board.GetFenString() + "(" + board.ZobristKey + ").");
        Console.ResetColor();
        return TT[zkey & mask].evaluation;
    }
}