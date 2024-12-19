<script lang="ts">
	import Header from "$project/Header.svelte";
	import Bracket from "$project/Bracket.svelte";
	import Footer from "$project/Footer.svelte";
	import { fade } from "svelte/transition";
	import type { Tournament } from "$project/types.js";
	import { tournamentData } from "$project/data.js";

	// Tournament data state
	let tournament: Tournament = $state(tournamentData);
	// Current selected round
	let selectedRound = $state(1);
</script>

<main class="flex min-h-screen w-full flex-col bg-gray-50 text-gray-900">
	<Header {selectedRound} />

	<!-- Main content -->
	<div class="container mx-auto px-4 py-8" in:fade={{ duration: 300 }}>
		<Bracket {tournament} {selectedRound} />
	</div>

	<Footer />
</main>
