<script lang="ts">
	import Hero from "$project/Hero.svelte";
	import Details from "$project/Details.svelte";
	import Navigation from "$project/Navigation.svelte";
	import Footer from "$project/Footer.svelte";
</script>

<div class="flex min-h-screen w-full flex-col bg-[#0f1115] text-white antialiased">
	<Navigation />
	<main class="flex-1">
		<Hero />
		<Details />
	</main>
	<Footer />
</div>
