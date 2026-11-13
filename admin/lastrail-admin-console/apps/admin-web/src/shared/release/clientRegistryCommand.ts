import type { QueryClient } from "@tanstack/react-query";
import { adminApi } from "../api/client";
import type { ClientRegistry, RegistryEditContext, RegistryInput, RegistryTarget } from "./clientReleaseRegistry";

export const registryCommandKey = (target: RegistryTarget) => ["client-release-command", target] as const;

// The app's QueryClient owns pending work across route remounts. A target's
// document-wide revision requires serial confirmation; never queue stale inputs.
export async function executeRegistryCommand(client: QueryClient, state: ClientRegistry, context: RegistryEditContext, input: RegistryInput): Promise<ClientRegistry> {
  const mutationKey = registryCommandKey(context.target);
  if (client.isMutating({ mutationKey })) throw new Error("같은 대상의 저장 처리 중 · 완료 후 최신 정책을 확인하세요.");
  const mutation = client.getMutationCache().build(client, {
    mutationKey,
    retry: false,
    networkMode: "always", // Preserve immediate API failure; never replay offline input on reconnect.
    mutationFn: () => adminApi.updateClientReleaseRegistry(state, context, input),
    onSettled: async () => {
      // Cache ownership survives the View. Only currently mounted observers
      // refresh; an exited page does not start follow-up requests.
      await client.invalidateQueries({ queryKey: ["current-version"], refetchType: "none" });
      await client.invalidateQueries({ queryKey: ["client-release-registry", context.target], exact: true });
    }
  });
  return mutation.execute(undefined);
}
