<script lang="ts">
	import Hero from "./Hero.svelte";
	import Tournaments from "./Tournaments.svelte";
	import Features from "./Features.svelte";
	import { fade } from "svelte/transition";

	// State to handle loading
	let loading: boolean = $state(true);

	// Simulate loading
	setTimeout(() => {
		loading = false;
	}, 500);
</script>

{#if loading}
	<div class="flex h-screen w-full items-center justify-center bg-[#1a1a1a] text-white" transition:fade>
		<div class="animate-spin text-4xl">🎮</div>
	</div>
{:else}
	<Hero />
	<Tournaments />
	<Features />
{/if}
