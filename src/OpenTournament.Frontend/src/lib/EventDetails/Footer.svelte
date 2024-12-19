<script>
	import { Twitter, Twitch, Discord } from "lucide-svelte";
</script>

<footer class="border-t border-white/10 bg-[#151820] py-12">
	<div class="mx-auto max-w-7xl px-4">
		<div class="flex flex-col items-center justify-between gap-8 md:flex-row">
			<div class="text-center md:text-left">
				<h3 class="text-xl font-bold text-[#ff6b6b]">CEOtaku 2024</h3>
				<p class="mt-2 text-sm text-gray-400">The Premier Anime Fighting Game Tournament</p>
			</div>

			<div class="flex gap-6">
				<a href="https://twitter.com/ceogaming" target="_blank" class="hover:text-[#ff6b6b]">
					<Twitter class="h-6 w-6" />
				</a>
				<a href="https://twitch.tv/ceogaming" target="_blank" class="hover:text-[#ff6b6b]">
					<Twitch class="h-6 w-6" />
				</a>
				<a href="#" class="hover:text-[#ff6b6b]">
					<Discord class="h-6 w-6" />
				</a>
			</div>
		</div>

		<div class="mt-8 text-center text-sm text-gray-400">
			<p>&copy; 2024 CEOtaku. All rights reserved.</p>
		</div>
	</div>
</footer>
