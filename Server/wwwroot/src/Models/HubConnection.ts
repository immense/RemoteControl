export type HubConnection = {
    start: () => Promise<any>;
    onclose: (callback: () => any) => any;
    state: string;
    invoke: (...rest) => any;
    stop: () => any;
}
