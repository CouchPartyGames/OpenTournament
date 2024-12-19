<script lang="ts">
	import { slide } from "svelte/transition";
	import type { Tournament } from "$project/types.js";

	// Props for tournament data and selected round
	let { tournament, selectedRound }: { tournament: Tournament; selectedRound: number } = $props();

	// Filter games by round
	const games = $derived(tournament.games.filter((game) => game.round === selectedRound));
</script>

<div class="grid gap-8 md:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4">
	{#each games as game (game.id)}
		<div class="rounded-lg border bg-white p-4 shadow-sm" transition:slide>
			<div class="text-sm font-medium text-gray-500">Game {game.id}</div>

			<!-- Team matchup -->
			<div class="mt-3 space-y-3">
				{#each game.teams as team}
					<div
						class="flex items-center justify-between gap-4 rounded-md p-2
                {team.winner ? 'bg-blue-50' : 'hover:bg-gray-50'}"
					>
						<div class="flex items-center gap-3">
							<img src={team.logo || "$assets/ui-placeholder.png"} alt={team.name} class="h-8 w-8 rounded-full object-cover" />
							<div>
								<div class="font-medium">{team.name}</div>
								<div class="text-sm text-gray-500">Seed {team.seed}</div>
							</div>
						</div>
						<div class="text-lg font-bold">{team.score}</div>
					</div>
				{/each}
			</div>

			<div class="mt-4 text-sm text-gray-500">
				{game.location} • {game.date}
			</div>
		</div>
	{/each}
</div>
