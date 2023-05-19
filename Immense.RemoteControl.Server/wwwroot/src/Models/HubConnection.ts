import { HubConnectionState } from "../Enums/HubConnectionState.js";
import { IStreamResult } from "../Stream.js";

export type HubConnection = {
    start: () => Promise<any>;
    onclose: (callback: () => any) => any;
    state: HubConnectionState;
    invoke<T = void>(...rest): Promise<T>;
    stop(): Promise<void>;
    stream<T = any>(methodName: string, ...args: any[]): IStreamResult<T>;
}
