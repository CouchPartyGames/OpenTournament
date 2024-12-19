<script lang="ts">
	import { Search, Menu, X } from "lucide-svelte";

	// Mobile menu state
	let isMenuOpen: boolean = $state(false);
</script>

<nav class="fixed z-50 w-full border-b border-white/10 bg-[#1a1a1a]/95 backdrop-blur">
	<div class="mx-auto flex max-w-7xl items-center justify-between px-4 py-4">
		<div class="flex items-center gap-10">
			<a href="#" class="text-2xl font-bold">start.gg</a>
			<div class="hidden items-center gap-6 lg:flex">
				<a href="#tournaments" class="hover:text-primary">Tournaments</a>
				<a href="#features" class="hover:text-primary">Features</a>
				<a href="#" class="hover:text-primary">Communities</a>
			</div>
		</div>

		<div class="hidden items-center gap-4 lg:flex">
			<div class="relative">
				<Search class="absolute left-3 top-2.5 h-5 w-5 text-gray-400" />
				<input type="text" placeholder="Search tournaments..." class="w-64 rounded-full bg-white/10 pl-10 pr-4 py-2 focus:outline-none focus:ring-2 focus:ring-white/20" />
			</div>
			<button class="rounded-full bg-blue-600 px-6 py-2 font-semibold hover:bg-blue-700">Sign In</button>
		</div>

		<button class="lg:hidden" onclick={() => (isMenuOpen = !isMenuOpen)}>
			{#if isMenuOpen}
				<X class="h-6 w-6" />
			{:else}
				<Menu class="h-6 w-6" />
			{/if}
		</button>
	</div>

	{#if isMenuOpen}
		<div class="lg:hidden" transition:fade>
			<div class="space-y-4 p-4">
				<a href="#tournaments" class="block hover:text-primary">Tournaments</a>
				<a href="#features" class="block hover:text-primary">Features</a>
				<a href="#" class="block hover:text-primary">Communities</a>
				<div class="relative">
					<Search class="absolute left-3 top-2.5 h-5 w-5 text-gray-400" />
					<input type="text" placeholder="Search tournaments..." class="w-full rounded-full bg-white/10 pl-10 pr-4 py-2 focus:outline-none focus:ring-2 focus:ring-white/20" />
				</div>
				<button class="w-full rounded-full bg-blue-600 px-6 py-2 font-semibold hover:bg-blue-700">Sign In</button>
			</div>
		</div>
	{/if}
</nav>
