using ChessChallenge.API;
using System.Collections.Generic;
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
    */
    public Move Think(Board board, Timer timer)
    {
        // The array of lists of possible moves, in which each list is a different depth of the search depending on where it is 
        List<Move>[] moves = new List<Move>[2];
        // initializes the first list of possible moves and captures, with a search depth of 1
        moves[0] = board.GetLegalMoves().ToList();
        moves[1]= board.GetLegalMoves(true).ToList();
        PieceList[] pieces = board.GetAllPieceLists();
        int[] index = new int[5];
        for(int i = 0; i < moves[0].Count; i++) {
            board.MakeMove(moves[0][i]);
            if(board.IsInCheckmate()) {
                return moves[0][i];
            }
            board.UndoMove(moves[0][i]);
            if(board.IsWhiteToMove) {
                for(int j = 0; j < 6; j++) {
                    index[3] += pieces[j].Count;
                }
                for(int j = 6; j < 12; j++) {
                    index[4] += pieces[j].Count;
                }
                index[2] += index[3] / index[4] * 8;
            } else {
                for(int j = 0; j < 6; j++) {
                    index[4] += pieces[j].Count;
                }
                for(int j = 6; j < 12; j++) {
                    index[3] += pieces[j].Count;
                }
                index[2] += index[4] / index[3] * 8;
            }
            index[2] += (moves[1].Count * 2) + moves[0].Count;
            if(index[2] > index[1]) {
                index[0] = i;
                index[1] = index[2];
            }
        }
        if(moves[0][index[0]] != Move.NullMove) {
            return moves[0][index[0]];
        } else {
            return moves[0][0];
        }
    }
}