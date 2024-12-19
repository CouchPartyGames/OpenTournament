<script lang="ts">
	import { Menu, X } from "lucide-svelte";

	// Mobile menu state
	let isOpen: boolean = $state(false);

	const links = [
		{ href: "#overview", label: "Overview" },
		{ href: "#details", label: "Details" },
		{ href: "#schedule", label: "Schedule" },
		{ href: "#registration", label: "Registration" },
	];
</script>

<nav class="fixed top-0 z-50 w-full border-b border-white/10 bg-[#0f1115]/80 backdrop-blur">
	<div class="mx-auto flex max-w-7xl items-center justify-between px-4 py-4">
		<a href="#" class="text-xl font-bold text-[#ff6b6b]">CEOtaku 2024</a>

		<!-- Desktop Navigation -->
		<div class="hidden items-center gap-6 md:flex">
			{#each links as link}
				<a href={link.href} class="text-sm hover:text-[#ff6b6b]">{link.label}</a>
			{/each}
			<a href="#register" class="rounded-full bg-[#ff6b6b] px-6 py-2 text-sm font-medium hover:bg-[#ff6b6b]/90">Register Now</a>
		</div>

		<!-- Mobile menu button -->
		<button onclick={() => (isOpen = !isOpen)} class="md:hidden">
			{#if isOpen}
				<X class="h-6 w-6" />
			{:else}
				<Menu class="h-6 w-6" />
			{/if}
		</button>
	</div>

	<!-- Mobile Navigation -->
	{#if isOpen}
		<div class="border-t border-white/10 px-4 py-4 md:hidden">
			{#each links as link}
				<a href={link.href} class="block py-2 text-sm hover:text-[#ff6b6b]">{link.label}</a>
			{/each}
			<a href="#register" class="mt-4 block rounded-full bg-[#ff6b6b] px-6 py-2 text-center text-sm font-medium hover:bg-[#ff6b6b]/90">Register Now</a>
		</div>
	{/if}
</nav>
