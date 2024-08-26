<script>
	import TournamentCard from "$lib/TournamentCard.svelte";

    let cards = [ 1, 2 , 3]
</script>
{#each cards as card }
    <TournamentCard />
{/each}