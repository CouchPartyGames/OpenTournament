<script lang="ts">
	import Nav from "$project/Nav.svelte";
	import Hero from "$project/Hero.svelte";
	import Tournaments from "$project/Tournaments.svelte";
	import Features from "$project/Features.svelte";
	import Footer from "$project/Footer.svelte";
	import { fade } from "svelte/transition";

	// State to handle loading
	let loading: boolean = $state(true);

	// Simulate loading
	setTimeout(() => {
		loading = false;
	}, 1000);
</script>

{#if loading}
	<div class="flex h-screen w-full items-center justify-center bg-[#1a1a1a] text-white" transition:fade>
		<div class="animate-spin text-4xl">🎮</div>
	</div>
{:else}
	<div class="flex min-h-screen w-full flex-col bg-[#1a1a1a] text-white">
		<Nav />
		<main class="flex-1">
			<Hero />
			<Tournaments />
			<Features />
		</main>
		<Footer />
	</div>
{/if}
