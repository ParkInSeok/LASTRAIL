import {AdminSessionResources} from './AdminSessionResources';
import {ReviewLifetime} from '../liveops/ReviewLifetime';
import { createContext, type ReactNode, useContext,useEffect,useState } from "react";
import { useQuery } from "@tanstack/react-query";
import type { BootstrapResponse } from "@lastrail/admin-contracts";
import { adminApi } from "../api/client";

type AdminContextValue = {
  session?:AdminSessionResources;
  sessionActive?:boolean;
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

  const identity=query.data;const scope=identity?JSON.stringify([identity.environment,identity.admin.id,identity.admin.role,[...identity.admin.permissions].sort()]):'unavailable';
  return <ScopedAdminProvider key={scope} value={{bootstrap:query.data,isLoading:query.isLoading,error:query.error}}>{children}</ScopedAdminProvider>;
}
function ScopedAdminProvider({value,children}:{value:AdminContextValue;children:ReactNode}){
 const [session]=useState(()=>new AdminSessionResources(crypto.randomUUID()));
 const [lifetime]=useState(()=>new ReviewLifetime(()=>session.dispose()));
 const [,changed]=useState(0);
 useEffect(()=>{const error=value.error;if(error&&'status'in error&&(error.status===401||error.status===403))session.revoke();},[value.error,session]);
 useEffect(()=>{const release=lifetime.enter(),off=session.subscribe(()=>changed(v=>v+1));return()=>{off();release();};},[session,lifetime]);
 return <AdminContext.Provider value={{...value,session,sessionActive:session.active}}>{children}</AdminContext.Provider>;
}

export function useAdmin(): AdminContextValue {
  const value = useContext(AdminContext);
  if (!value) throw new Error("useAdmin must be used inside AdminProvider.");
  return value;
}

