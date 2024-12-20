import { PUBLIC_HTTP_PROTOCOL, PUBLIC_HTTP_HOST } from '$env/static/public';

export const load = async () => {
	
	console.log('protocol: ' + PUBLIC_HTTP_PROTOCOL + "://" + PUBLIC_HTTP_HOST);
	//const host = "http://localhost:8080"
	const host = "https://dummyjson.com"
	const resp = await fetch(host + '/products');
	const data = await resp.json();
	if (data) {
		//console.log(data);
		return { data };
	}
};