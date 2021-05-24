import { gql } from '@apollo/client'

export const LATEST_QUERY = gql`
  query Latests($skip: Int! $take: Int!) {
    latests(skip: $skip take: $take) {
      pageInfo{
        hasNextPage
        hasPreviousPage
      }
      items{
        nodeId
        count
        recordTime
      }
      totalCount
    }
  }
`;

export const COUNT_SUBSCRIPTION = gql`
  subscription OnRecorded {
    onRecorded {
      nodeId
      count
      recordTime
    }
  }
`;

export const LOG_QUERY = gql`
  query Logs($skip: Int! $take: Int!) {
    logs(skip: $skip take: $take order:{id:ASC}) {
      pageInfo{
        hasNextPage
        hasPreviousPage
      }
      items{
        id
        nodeId
        count
        recordTime
      }
      totalCount
    }
  }
`;

export const LOGCHART_QUERY = gql`
  query LogsForChart {
    logsForChart {
        id
        nodeId
        count
        recordTime
    }
  }
`;

export const MOVIE_SUBSCRIPTION = gql`
  subscription OnStream {
    onStream
  }
`;