export * from './gql'

export interface PageInfo {
  hasNextPage: boolean
  hasPreviousPage: boolean
}

export interface Counter {
  nodeId: string
  count: number
  localRecordTime: string
  utcRecordTime: string
}

export interface Log {
  id: number
  nodeId: string
  count: number
  localRecordTime: string
  utcRecordTime: string
}

export interface Chart {
  time: string
  value: number
}

export interface OffsetPaginationCount {
  pageInfo: PageInfo
  items: Counter[]
  totalCount: number
}

export interface OffsetPaginationLog {
  pageInfo: PageInfo
  items: Log[]
  totalCount: number
}

export interface OffsetPaginationVars {
  skip: number
  take: number
}

export interface Latests {
  latests: OffsetPaginationCount
  onRecorded: Counter
}

export interface Logs {
  logs: OffsetPaginationLog
}

export interface LogsForChart {
  logsForChart: Log[]
}