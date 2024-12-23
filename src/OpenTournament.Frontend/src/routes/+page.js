import { PUBLIC_HTTP_PROTOCOL, PUBLIC_HTTP_HOST, PUBLIC_HTTP_PORT } from '$env/static/public';

export const load = async () => {
	
	console.log('protocol: ' + PUBLIC_HTTP_PROTOCOL + "://" + PUBLIC_HTTP_HOST + ":" + PUBLIC_HTTP_PORT);
	const host = PUBLIC_HTTP_PROTOCOL + "://" + PUBLIC_HTTP_HOST + ":" + PUBLIC_HTTP_PORT;
	//const host = "https://dummyjson.com"
	const resp = await fetch(host + '/events/v1');
	const data = await resp.json();
	if (data) {
		console.log(data);
		return { data };
	}
};