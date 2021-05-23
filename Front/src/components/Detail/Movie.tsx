import React from 'react'
import { useSubscription } from '@apollo/client'
import { CircularProgress } from '@material-ui/core'
import { MOVIE_SUBSCRIPTION } from '../Common/types'

const Movie: React.FC<any> = (props: any) => {
  const { data, loading } = useSubscription(MOVIE_SUBSCRIPTION)
  return (
    <>
      {loading ? <CircularProgress /> : <img alt='stream' src={`data:image/png;base64,${data.onStream}`} />}
    </>
  )
}

export { Movie }