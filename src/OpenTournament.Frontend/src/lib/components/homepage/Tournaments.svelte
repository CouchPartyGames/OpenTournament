<script lang="ts">
	import { Calendar, Users, Trophy } from "lucide-svelte";

	// Featured tournaments data
	type Tournament = {
		title: string;
		game: string;
		date: string;
		players: number;
		prize: string;
		image: string;
	};

	let tournaments: Tournament[] = $state([
		{
			title: "Super Smash Ultimate Championship",
			game: "Super Smash Bros. Ultimate",
			date: "Dec 15, 2023",
			players: 128,
			prize: "$10,000",
			image: "https://svelte0.dev/images/ui-placeholder.png",
			uri: '/event/super-smash-ultimate-championship',
		},
		{
			title: "Street Fighter 6 Pro League",
			game: "Street Fighter 6",
			date: "Dec 20, 2023",
			players: 64,
			prize: "$5,000",
			image: "https://svelte0.dev/images/ui-placeholder.png",
			uri: '/event/super-smash-ultimate-championship',
		},
		{
			title: "Tekken 8 World Tour",
			game: "Tekken 8",
			date: "Dec 25, 2023",
			players: 256,
			prize: "$15,000",
			image: "https://svelte0.dev/images/ui-placeholder.png",
			uri: '/event/super-smash-ultimate-championship',
		},
	]);
</script>

<section id="tournaments" class="py-20">
	<div class="mx-auto max-w-7xl px-4">
		<h2 class="mb-12 text-center text-3xl font-bold md:text-4xl">Featured Tournaments</h2>

		<div class="grid gap-6 md:grid-cols-2 lg:grid-cols-3">
			{#each tournaments as tournament}
				<div class="group rounded-xl bg-white/5 p-4 transition-all hover:bg-white/10">
					<a href="{tournament.uri}">
						<img src={tournament.image} alt={tournament.title} class="mb-4 h-48 w-full rounded-lg object-cover" />
					</a>
					<h3 class="mb-2 text-xl font-semibold"><a href="{tournament.uri}">{tournament.title}</a></h3>
					<p class="mb-4 text-sm text-gray-400">{tournament.game}</p>

					<div class="flex flex-wrap gap-4 text-sm">
						<div class="flex items-center gap-2">
							<Calendar class="h-4 w-4" />
							{tournament.date}
						</div>
						<div class="flex items-center gap-2">
							<Users class="h-4 w-4" />
							{tournament.players} Players
						</div>
						<div class="flex items-center gap-2">
							<Trophy class="h-4 w-4" />
							{tournament.prize}
						</div>
					</div>
				</div>
			{/each}
		</div>
	</div>
</section>
