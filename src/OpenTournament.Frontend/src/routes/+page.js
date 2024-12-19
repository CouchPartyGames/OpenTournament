export const load = async ({fetch}) => {
	const resp = await fetch('https://dummyjson.com/products');
	const data = await resp.json();
	console.log(data);
	return { data };
};
