export type Team = {
	id: number;
	name: string;
	seed: number;
	score: number;
	logo?: string;
	winner: boolean;
};

export type Game = {
	id: number;
	round: number;
	teams: Team[];
	date: string;
	location: string;
};

export type Tournament = {
	games: Game[];
};
