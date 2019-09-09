namespace Shared

type Character = { Name: string; Actor: string }

type BondFilm = {
    SequenceId: int
    Title: string
    Synopsis: string
    Bond: string
    M: string option
    Q: string option
    TheEnemy: Character list
    TheGirls: Character list
}

