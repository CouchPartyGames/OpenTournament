<script lang="ts">
	const games = ["GUILTY GEAR -STRIVE-", "UNDER NIGHT IN-BIRTH II Sys:Celes", "MELTY BLOOD: TYPE LUMINA", "Street Fighter 6", "GRANBLUE FANTASY: Versus Rising", "DNF DUEL"];

	const schedule = [
		{ day: "Friday", date: "Sep 27", events: ["Registration Opens", "Casual Gaming", "Side Tournaments"] },
		{ day: "Saturday", date: "Sep 28", events: ["Main Tournament Pools", "Artist Alley", "Vendor Hall"] },
		{ day: "Sunday", date: "Sep 29", events: ["Top 8 Finals", "Closing Ceremony", "Awards"] },
	];
</script>

<section class="py-20">
	<div class="mx-auto max-w-7xl px-4">
		<!-- Featured Games -->
		<div class="mb-20">
			<h2 class="mb-8 text-center text-3xl font-bold">Featured Games</h2>
			<div class="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
				{#each games as game}
					<div class="rounded-lg border border-white/10 bg-[#151820] p-6 text-center">
						<h3 class="text-lg font-semibold">{game}</h3>
					</div>
				{/each}
			</div>
		</div>

		<!-- Schedule -->
		<div class="mb-20">
			<h2 class="mb-8 text-center text-3xl font-bold">Event Schedule</h2>
			<div class="grid grid-cols-1 gap-6 md:grid-cols-3">
				{#each schedule as { day, date, events }}
					<div class="rounded-lg border border-white/10 bg-[#151820] p-6">
						<h3 class="mb-2 text-xl font-bold text-[#ff6b6b]">{day}</h3>
						<p class="mb-4 text-sm text-gray-400">{date}</p>
						<ul class="space-y-2">
							{#each events as event}
								<li>{event}</li>
							{/each}
						</ul>
					</div>
				{/each}
			</div>
		</div>

		<!-- Registration Info -->
		<div id="register" class="rounded-lg border border-white/10 bg-[#151820] p-8 text-center">
			<h2 class="mb-4 text-3xl font-bold">Registration</h2>
			<p class="mb-6 text-gray-400">Early bird registration is now open! Secure your spot and save with our limited-time pricing.</p>
			<a href="#" class="inline-block rounded-full bg-[#ff6b6b] px-8 py-3 font-semibold hover:bg-[#ff6b6b]/90">Register Now</a>
		</div>
	</div>
</section>
