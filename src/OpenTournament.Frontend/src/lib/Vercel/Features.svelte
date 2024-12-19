<script>
	import { Trophy, Users, BarChart, Shield } from "lucide-svelte";
</script>

<section id="features" class="bg-white/5 py-20">
	<div class="mx-auto max-w-7xl px-4">
		<h2 class="mb-12 text-center text-3xl font-bold md:text-4xl">Platform Features</h2>

		<div class="grid gap-8 md:grid-cols-2 lg:grid-cols-4">
			<div class="rounded-xl bg-white/5 p-6">
				<Trophy class="mb-4 h-12 w-12 text-blue-500" />
				<h3 class="mb-2 text-xl font-semibold">Tournament Organization</h3>
				<p class="text-gray-400">Create and manage tournaments with powerful tools and customization options.</p>
			</div>

			<div class="rounded-xl bg-white/5 p-6">
				<Users class="mb-4 h-12 w-12 text-blue-500" />
				<h3 class="mb-2 text-xl font-semibold">Community Building</h3>
				<p class="text-gray-400">Connect with players, create teams, and build your gaming community.</p>
			</div>

			<div class="rounded-xl bg-white/5 p-6">
				<BarChart class="mb-4 h-12 w-12 text-blue-500" />
				<h3 class="mb-2 text-xl font-semibold">Statistics & Analytics</h3>
				<p class="text-gray-400">Track performance, view rankings, and analyze tournament data.</p>
			</div>

			<div class="rounded-xl bg-white/5 p-6">
				<Shield class="mb-4 h-12 w-12 text-blue-500" />
				<h3 class="mb-2 text-xl font-semibold">Anti-Cheat System</h3>
				<p class="text-gray-400">Ensure fair play with our advanced anti-cheat protection system.</p>
			</div>
		</div>
	</div>
</section>
