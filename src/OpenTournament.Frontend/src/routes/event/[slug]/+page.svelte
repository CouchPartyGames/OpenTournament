<script>
	export const prerender = true;

    const siteName = 'ggparty.online';
    let { data } = $props();
    import  Details from '$lib/components/details/index.js';
</script>
<svelte:head>
    <title>{siteName}</title>
    <meta name="description" content="Tournaments and Brackets for competition" />
    <meta name="keywords" content="Tournament, Bracket, Competition" />
</svelte:head>
<Details></Details>

