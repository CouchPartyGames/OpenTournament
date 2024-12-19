<script lang="ts">
	import { Calendar, MapPin, Users } from "lucide-svelte";
</script>

<section class="relative mt-16 overflow-hidden bg-[#151820] py-20">
	<div class="mx-auto max-w-7xl px-4">
		<div class="relative z-10 flex flex-col items-center text-center">
			<h1 class="mb-6 text-4xl font-bold leading-tight md:text-6xl">CEOtaku 2024</h1>
			<p class="mb-8 max-w-2xl text-lg text-gray-400">The largest Anime Fighting Game tournament series returns to Orlando, Florida!</p>

			<div class="mb-8 flex flex-wrap justify-center gap-6 text-sm">
				<div class="flex items-center gap-2">
					<Calendar class="h-5 w-5 text-[#ff6b6b]" />
					<span>September 27-29, 2024</span>
				</div>
				<div class="flex items-center gap-2">
					<MapPin class="h-5 w-5 text-[#ff6b6b]" />
					<span>Wyndham Orlando Resort, FL</span>
				</div>
				<div class="flex items-center gap-2">
					<Users class="h-5 w-5 text-[#ff6b6b]" />
					<span>2000+ Attendees Expected</span>
				</div>
			</div>

			<a href="#register" class="rounded-full bg-[#ff6b6b] px-8 py-3 text-lg font-semibold hover:bg-[#ff6b6b]/90">Register Now</a>
		</div>
	</div>
</section>
