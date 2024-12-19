<script lang="ts">
	import Hero from "./Hero.svelte";
	import Details from "./Details.svelte";
	import Navigation from "./Navigation.svelte";
	import Footer from "../Footer.svelte";
</script>

<div class="flex min-h-screen w-full flex-col bg-[#0f1115] text-white antialiased">
	<Navigation />
	<main class="flex-1">
		<Hero />
		<Details />
	</main>
	<Footer />
</div>
