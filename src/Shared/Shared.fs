namespace Shared

type Character = { Name: string; Actor: string; ImageURI: string option }

type BondFilm = {
    SequenceId: int
    Title: string
    Synopsis: string
    Bond: Character option
    M: Character option
    Q: Character option
    TheEnemy: Character list
    TheGirls: Character list
}

