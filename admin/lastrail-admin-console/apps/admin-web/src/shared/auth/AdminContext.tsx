import { createContext, type ReactNode, useContext } from "react";
import { useQuery } from "@tanstack/react-query";
import type { BootstrapResponse } from "@lastrail/admin-contracts";
import { adminApi } from "../api/client";

type AdminContextValue = {
  bootstrap?: BootstrapResponse;
  isLoading: boolean;
  error: Error | null;
};

const AdminContext = createContext<AdminContextValue | undefined>(undefined);

export function AdminProvider({ children }: { children: ReactNode }) {
  const query = useQuery({
    queryKey: ["bootstrap"],
    queryFn: adminApi.bootstrap,
    retry: false
  });

  return (
    <AdminContext.Provider
      value={{
        bootstrap: query.data,
        isLoading: query.isLoading,
        error: query.error
      }}
    >
      {children}
    </AdminContext.Provider>
  );
}

export function useAdmin(): AdminContextValue {
  const value = useContext(AdminContext);
  if (!value) throw new Error("useAdmin must be used inside AdminProvider.");
  return value;
}

