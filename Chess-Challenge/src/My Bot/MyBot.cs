using ChessChallenge.API;
using System;
using System.Numerics;
using System.Collections;
using System.Collections.Generic;
using ChessChallenge.Application;

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
    */

    int[] pieceValues = {0, 100, 310, 330, 500, 1000, 50000};

    int universalDepth;
    Move selectedMove;

    readonly ulong[,] mg_psqts = {
        {0xFFFFBEE41FE25900, 0x000017002A03A700, 0x0000101D1FADDE00, 0xFFFFF10C32DACF00, 0xFFFFC83B3EE73D00, 0xFFFFDE2C08D59F00, 0x0000022B1F06F100, 0x00000D2D2AF79500},
        {0x00001CE81AE5B762, 0xFFFFFED9200FD786, 0xFFFFEBFB39EE483D, 0xFFFFF9013DF3245F, 0xFFFFF7F0501E1744, 0xFFFFFC39433B3E7E, 0xFFFFDA1C1A120722, 0xFFFFE3362BD0EEF5},
        {0xFFFFF6F2FAEFD0FA, 0x000017EF13253C07, 0x000002071A2B251A, 0xFFFFF0082428411F, 0xFFFFEC1D11235441, 0x000006382D328138, 0x0000162F3D254919, 0xFFFFEA390FFE2BEC},
        {0xFFFFEEE4E7FBF6F2, 0xFFFFEBE4F505110D, 0xFFFFF3F007131306, 0xFFFFE4F01A323515, 0xFFFFE1FF18252517, 0xFFFFE7112325450C, 0xFFFFF1FDF8071211, 0xFFFFDC00EBFE15E9},
        {0xFFFFCEF6DBF9F2E5, 0xFFFFFEE5E60D03FE, 0xFFFFE4F6F40D0FFB, 0xFFFFD8F5FF1A0D0C, 0xFFFFD1FE09221C11, 0xFFFFD3FBF90C1306, 0xFFFFDF03060A150A, 0xFFFFCCFCE903F7E7},
        {0xFFFFF1F1D2FFE8E6, 0xFFFFF201E70EF6FC, 0xFFFFE9F4F00F0BFC, 0xFFFFD1FDEF0F09F6, 0xFFFFD3FB030E1303, 0xFFFFE202001B1103, 0xFFFFF10DFB121921, 0xFFFFE504DF09EFF4},
        {0x000000DCD403E2DD, 0x000006F7F00ECAFF, 0xFFFFF80AEC0FF3EC, 0xFFFFC001F6FFFCE9, 0xFFFFD507FF06FEF1, 0xFFFFF00F0B151218, 0x000008FCFA20F226, 0x00000800B900ECEA},
        {0xFFFFF0FEECDE9700, 0x000023EDF2FCEB00, 0x00000BF700F1C600, 0xFFFFCA0A10EADF00, 0x000007F10FF2EF00, 0xFFFFE3E706F3E400, 0x000017E0DAD8ED00, 0x00000DCDE5EAE900}
    };
    readonly ulong[,] eg_psqts = {
        {0xFFFFBEE41FE25900, 0x000017002A03A700, 0x0000101D1FADDE00, 0xFFFFF10C32DACF00, 0xFFFFC83B3EE73D00, 0xFFFFDE2C08D59F00, 0x0000022B1F06F100, 0x00000D2D2AF79500},
        {0x00001CE81AE5B762, 0xFFFFFED9200FD786, 0xFFFFEBFB39EE483D, 0xFFFFF9013DF3245F, 0xFFFFF7F0501E1744, 0xFFFFFC39433B3E7E, 0xFFFFDA1C1A120722, 0xFFFFE3362BD0EEF5},
        {0xFFFFF6F2FAEFD0FA, 0x000017EF13253C07, 0x000002071A2B251A, 0xFFFFF0082428411F, 0xFFFFEC1D11235441, 0x000006382D328138, 0x0000162F3D254919, 0xFFFFEA390FFE2BEC},
        {0xFFFFEEE4E7FBF6F2, 0xFFFFEBE4F505110D, 0xFFFFF3F007131306, 0xFFFFE4F01A323515, 0xFFFFE1FF18252517, 0xFFFFE7112325450C, 0xFFFFF1FDF8071211, 0xFFFFDC00EBFE15E9},
        {0xFFFFCEF6DBF9F2E5, 0xFFFFFEE5E60D03FE, 0xFFFFE4F6F40D0FFB, 0xFFFFD8F5FF1A0D0C, 0xFFFFD1FE09221C11, 0xFFFFD3FBF90C1306, 0xFFFFDF03060A150A, 0xFFFFCCFCE903F7E7},
        {0xFFFFF1F1D2FFE8E6, 0xFFFFF201E70EF6FC, 0xFFFFE9F4F00F0BFC, 0xFFFFD1FDEF0F09F6, 0xFFFFD3FB030E1303, 0xFFFFE202001B1103, 0xFFFFF10DFB121921, 0xFFFFE504DF09EFF4},
        {0x000000DCD403E2DD, 0x000006F7F00ECAFF, 0xFFFFF80AEC0FF3EC, 0xFFFFC001F6FFFCE9, 0xFFFFD507FF06FEF1, 0xFFFFF00F0B151218, 0x000008FCFA20F226, 0x00000800B900ECEA},
        {0xFFFFF0FEECDE9700, 0x000023EDF2FCEB00, 0x00000BF700F1C600, 0xFFFFCA0A10EADF00, 0x000007F10FF2EF00, 0xFFFFE3E706F3E400, 0x000017E0DAD8ED00, 0x00000DCDE5EAE900}
    };



    public Move Think(Board board, Timer timer) {
        universalDepth = 3;
        Evaluate(board, universalDepth, -99999999, 99999999, board.IsWhiteToMove ? 1 : -1, board.PlyCount);

        return selectedMove;
    }
    int Evaluate(Board board, int depth, int alpha, int beta, int color, int plyCount) {
        Move[] allMoves = board.GetLegalMoves();
        if(board.IsDraw()) {
            return 0;
        }
        if(depth == 0 || allMoves.Length == 0) {
            int sum = 0;
            if(board.IsInCheckmate()) {
                return -10000000 + board.PlyCount - plyCount; // BIG NUMBER
            }
            for(int i = 1; i < 7; i++) {
               sum += (board.GetPieceList((PieceType)i, true).Count - board.GetPieceList((PieceType)i, false).Count) * pieceValues[i];
            }
            foreach(PieceList pieceList in board.GetAllPieceLists()) {
                foreach(Piece piece in pieceList) {
                    sum += GetPieceBonusScore(piece.PieceType, piece.IsWhite, piece.Square.File, piece.Square.Rank, board);
                }
            }
            return color * sum;
        }
        int recordEval = int.MinValue;
        foreach(Move move in allMoves) {
            board.MakeMove(move);
            int evaluation = -Evaluate(board, depth - 1, -beta, -alpha, -color, plyCount);
            board.UndoMove(move);
            if(recordEval < evaluation) {
                recordEval = evaluation;
                if (universalDepth == depth) {
                    selectedMove = move;
                }
            }
            alpha = Math.Max(alpha, recordEval);
            if(alpha >= beta) { 
                break;
            }
        }
        return recordEval;

    }

    private int GetPieceBonusScore(PieceType type, bool isWhite, int file, int rank, Board board) {
        type = (PieceType)(int)type-1;
        if(!isWhite) rank = 7 - rank;
        int unpackedData = 0;
        ulong bytemask = 0xFF;
        if(BitOperations.PopCount(board.AllPiecesBitboard) < 10) {
            unpackedData = (int)(eg_psqts[rank,file] & (bytemask << (int)type)) >> (int)type;
        } else {
            unpackedData = (int)(mg_psqts[rank,file] & (bytemask << (int)type)) >> (int)type;
        }
        if(!isWhite) unpackedData *= -1;
        return unpackedData;
    }
}