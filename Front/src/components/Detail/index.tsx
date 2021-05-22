import React from 'react'
import {
  Card,
  CardContent,
  CardHeader
} from '@material-ui/core';
import { Theme, makeStyles, createStyles } from '@material-ui/core/styles'
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
const Detail: React.FC<any> = (props: any) => {
  const { params } = props.match
  const classes = useStyles()
  return (
    <Card className={classes.root}>
      <CardHeader title="Detail" className={classes.cardheader} />
      <CardContent>nodeId: {params.id}</CardContent>
    </Card>
  )
}

export { Detail }