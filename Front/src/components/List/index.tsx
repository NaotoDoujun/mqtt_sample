import React from 'react'
import { useApolloClient } from '@apollo/client'
import { Card, CardHeader } from '@material-ui/core';
import { Theme, makeStyles, createStyles } from '@material-ui/core/styles'
import { LogList } from './LogList'
const useStyles = makeStyles((theme: Theme) =>
  createStyles({
    root: {
      padding: theme.spacing(1)
    },
    cardheader: {
      backgroundColor: theme.palette.primary.main,
      color: theme.palette.primary.contrastText
    },
  }))
const List: React.FC<any> = (props: any) => {
  const classes = useStyles()
  const client = useApolloClient()

  React.useEffect(() => {
    return () => {
      client.cache.evict({
        id: "Query",
        fieldName: "items"
      })
      client.cache.gc()
    }
  }, [client])

  return (
    <Card className={classes.root}>
      <CardHeader title="List" className={classes.cardheader} />
      <LogList />
    </Card>
  )
}

export { List }