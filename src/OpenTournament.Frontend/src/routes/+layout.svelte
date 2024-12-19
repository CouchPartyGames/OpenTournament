<script lang="ts">
	import '../app.css';
    import Footer from "$lib/components/Footer.svelte";
    import Nav from "$lib/components/Nav.svelte";
	let { children } = $props();
</script>

<div class="flex min-h-screen w-full flex-col bg-[#1a1a1a] text-white">
    <Nav />
    <main class="flex-1">
    {@render children()}
    </main>
    <Footer />
</div>