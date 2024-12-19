<script lang="ts">
	import { Basketball } from "lucide-svelte";

	// Get selected round as prop
	let { selectedRound }: { selectedRound: number } = $props();

	// Tournament rounds
	const rounds = [
		{ id: 1, name: "First Round" },
		{ id: 2, name: "Second Round" },
		{ id: 3, name: "Sweet 16" },
		{ id: 4, name: "Elite Eight" },
		{ id: 5, name: "Final Four" },
		{ id: 6, name: "Championship" },
	];
</script>

<header class="sticky top-0 z-50 w-full border-b bg-white shadow-sm">
	<div class="container mx-auto">
		<!-- Logo section -->
		<div class="flex items-center justify-between border-b px-4 py-4">
			<div class="flex items-center gap-2">
				<Basketball class="h-8 w-8 text-blue-600" />
				<h1 class="text-xl font-bold">NCAA Tournament Bracket</h1>
			</div>
		</div>

		<!-- Navigation -->
		<nav class="overflow-x-auto">
			<div class="flex min-w-max px-4">
				{#each rounds as round}
					<button
						class="border-b-2 px-4 py-3 text-sm font-medium transition-colors hover:bg-gray-50
                        {round.id === selectedRound ? 'border-blue-600 text-blue-600' : 'border-transparent text-gray-600'}"
						onclick={() => (selectedRound = round.id)}
					>
						{round.name}
					</button>
				{/each}
			</div>
		</nav>
	</div>
</header>
