<script>
	import { Github } from "lucide-svelte";

	const currentYear = new Date().getFullYear();
</script>

<footer class="mt-auto border-t bg-white">
	<div class="container mx-auto px-4 py-6">
		<div class="flex flex-col items-center justify-between gap-4 sm:flex-row">
			<div class="text-sm text-gray-600">
				© {currentYear} NCAA Tournament Bracket. All rights reserved.
			</div>

			<div class="flex items-center gap-4">
				<a href="https://github.com" target="_blank" class="text-gray-600 hover:text-gray-900">
					<Github class="h-5 w-5" />
				</a>
			</div>
		</div>
	</div>
</footer>
