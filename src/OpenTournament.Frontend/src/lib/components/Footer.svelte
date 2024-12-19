<script lang="ts">
	import { Github, Twitter } from "lucide-svelte";
	const siteName: string = 'ggparty.online';
	const currentYear: number = new Date().getFullYear();
</script>

<footer class="border-t border-white/10 bg-black/20">
	<div class="mx-auto max-w-7xl px-4 py-12">
		<div class="grid gap-8 md:grid-cols-4">
			<div>
				<h3 class="mb-4 text-lg font-bold">{siteName}</h3>
				<p class="text-sm text-gray-400">The ultimate tournament platform for competitive gaming communities.</p>
			</div>

			<div>
				<h4 class="mb-4 font-semibold">Quick Links</h4>
				<ul class="space-y-2 text-sm text-gray-400">
					<li><a href="/" class="hover:text-white">Home</a></li>
					<li><a href="#tournaments" class="hover:text-white">Tournaments</a></li>
					<li><a href="#features" class="hover:text-white">Features</a></li>
					<li><a href="#" class="hover:text-white">Communities</a></li>
				</ul>
			</div>

			<div>
				<h4 class="mb-4 font-semibold">Support</h4>
				<ul class="space-y-2 text-sm text-gray-400">
					<!-- <li><a href="#" class="hover:text-white">Help Center</a></li> -->
					<li><a href="/terms" class="hover:text-white">Terms of Service</a></li>
					<li><a href="/privacy" class="hover:text-white">Privacy Policy</a></li>
					<li><a href="/contact" class="hover:text-white">Contact Us</a></li>
				</ul>
			</div>

			<div>
				<h4 class="mb-4 font-semibold">Connect With Us</h4>
				<div class="flex gap-4">
					<a href="https://github.com" target="_blank" class="hover:text-blue-500">
						<Github class="h-6 w-6" />
					</a>
					<a href="https://twitter.com" target="_blank" class="hover:text-blue-500">
						<Twitter class="h-6 w-6" />
					</a>
				</div>
			</div>
		</div>

		<div class="mt-8 border-t border-white/10 pt-8 text-center text-sm text-gray-400">
			<p>&copy; {currentYear} {siteName}. All rights reserved.</p>
		</div>
	</div>
</footer>