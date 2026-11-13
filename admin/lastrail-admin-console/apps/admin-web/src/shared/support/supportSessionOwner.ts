import type {QueryClient} from '@tanstack/react-query';
import type {SupportWriteSession} from './SupportWriteSession';
/** In-memory ownership lasts until the account-scoped QueryClient is destroyed. */
export function ownSupportSession(client:QueryClient,actor:string,profile:string,create:()=>SupportWriteSession){
 const key=['support-write-session',actor,profile];
 client.setQueryDefaults(key,{gcTime:Infinity});
 const existing=client.getQueryData<SupportWriteSession>(key);if(existing)return existing;
 const session=create();client.setQueryData(key,session);return session;
}
