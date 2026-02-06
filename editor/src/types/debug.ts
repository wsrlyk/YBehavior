
export enum NodeState {
    Invalid = -1,
    Success = 0,
    Failure = 1,
    Break = 2,
    Running = 3,
}

export enum BreakpointType {
    None = 0,
    Breakpoint = 1,
    Logpoint = -1,
}

export interface NodeRunState {
    self: NodeState;
    final: NodeState;
}

export interface TreeRunInfo {
    treeName: string;
    nodeStates: Map<number, NodeRunState>;
    sharedVariables: Map<string, string>;
    localVariables: Map<string, string>;
    sharedVariableTimestamps: Map<string, number>;
    localVariableTimestamps: Map<string, number>;
}

export interface FSMRunInfo {
    fsmName: string;
    stateInfos: Map<number, number>;
}

export interface DebugMessage {
    header: string;
    content: string;
}

export interface TickResultData {
    mainData: string;
    fsmRunData: string;
    treeRunDatas: Map<string, {
        name: string;
        localData: string;
        runData: string;
    }>;
}
