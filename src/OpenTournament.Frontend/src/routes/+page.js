import { PUBLIC_BASE_URL } from '$env/static/public';

export const prerender = true;
export const load = async () => {
	
	const host = PUBLIC_BASE_URL;
	const resp = await fetch(host + '/events/v1');
	const data = await resp.json();
	if (data) {
		console.log(data);
		return { data };
	}
};